// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.Application.Tools;
using System;

namespace PackageUploader.Application.Test.Tools;

[TestClass]
public class Msixvc2DelegationGuardTest
{
    private string? _originalValue;
    private Mock<IParentProcessProvider> _parentProcessProviderMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _originalValue = Environment.GetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName);
        _parentProcessProviderMock = new Mock<IParentProcessProvider>();
    }

    [TestCleanup]
    public void Cleanup() =>
        Environment.SetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName, _originalValue);

    private Msixvc2DelegationGuard CreateGuard() => new(_parentProcessProviderMock.Object);

    [TestMethod]
    public void IsDelegatedInvocation_WhenVariableAbsent_IsFalse()
    {
        Environment.SetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName, null);

        Assert.IsFalse(CreateGuard().IsDelegatedInvocation);
    }

    [TestMethod]
    public void IsDelegatedInvocation_WhenVariableSet_IsTrue()
    {
        Environment.SetEnvironmentVariable(
            Msixvc2DelegationGuard.EnvironmentVariableName,
            Msixvc2DelegationGuard.EnvironmentVariableValue);

        Assert.IsTrue(CreateGuard().IsDelegatedInvocation);
    }

    /// <summary>
    /// The provider reports an extension when it can read the parent's module name and a bare process name
    /// otherwise, so both spellings of both MakePkg executables have to be recognized.
    /// </summary>
    [TestMethod]
    [DataRow("MakePkg.exe")]
    [DataRow("makepkg.exe")]
    [DataRow("MAKEPKG.EXE")]
    [DataRow("makepkg")]
    [DataRow("makepkg2.exe")]
    [DataRow("MakePkg2")]
    public void GetMakePkgParentProcessName_WhenParentIsMakePkg_ReturnsParentName(string parentFileName)
    {
        _parentProcessProviderMock.Setup(x => x.GetParentProcessFileName()).Returns(parentFileName);

        Assert.AreEqual(parentFileName, CreateGuard().GetMakePkgParentProcessName());
    }

    /// <summary>
    /// Guards against matching on a substring: an unrelated executable whose name merely contains "makepkg"
    /// must not be mistaken for the real tool, or ordinary uploads would start failing.
    /// </summary>
    [TestMethod]
    [DataRow("PackageUploader.exe")]
    [DataRow("cmd.exe")]
    [DataRow("makepkg-wrapper.exe")]
    [DataRow("mymakepkg.exe")]
    [DataRow("makepkg3.exe")]
    public void GetMakePkgParentProcessName_WhenParentIsNotMakePkg_ReturnsNull(string parentFileName)
    {
        _parentProcessProviderMock.Setup(x => x.GetParentProcessFileName()).Returns(parentFileName);

        Assert.IsNull(CreateGuard().GetMakePkgParentProcessName());
    }

    /// <summary>
    /// An unknown parent must read as "not known to be MakePkg" rather than blocking the upload, since the
    /// provider legitimately returns null whenever the OS declines the lookup.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetMakePkgParentProcessName_WhenParentUnknown_ReturnsNull(string? parentFileName)
    {
        _parentProcessProviderMock.Setup(x => x.GetParentProcessFileName()).Returns(parentFileName!);

        Assert.IsNull(CreateGuard().GetMakePkgParentProcessName());
    }
}
