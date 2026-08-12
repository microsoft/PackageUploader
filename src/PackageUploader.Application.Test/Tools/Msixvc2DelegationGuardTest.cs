// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Tools;
using System;

namespace PackageUploader.Application.Test.Tools;

[TestClass]
public class Msixvc2DelegationGuardTest
{
    private string? _originalValue;

    [TestInitialize]
    public void Initialize() =>
        _originalValue = Environment.GetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName);

    [TestCleanup]
    public void Cleanup() =>
        Environment.SetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName, _originalValue);

    [TestMethod]
    public void IsDelegatedInvocation_WhenVariableAbsent_IsFalse()
    {
        Environment.SetEnvironmentVariable(Msixvc2DelegationGuard.EnvironmentVariableName, null);

        Assert.IsFalse(new Msixvc2DelegationGuard().IsDelegatedInvocation);
    }

    [TestMethod]
    public void IsDelegatedInvocation_WhenVariableSet_IsTrue()
    {
        Environment.SetEnvironmentVariable(
            Msixvc2DelegationGuard.EnvironmentVariableName,
            Msixvc2DelegationGuard.EnvironmentVariableValue);

        Assert.IsTrue(new Msixvc2DelegationGuard().IsDelegatedInvocation);
    }
}
