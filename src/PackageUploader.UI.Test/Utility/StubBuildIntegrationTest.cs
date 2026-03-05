using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace PackageUploader.UI.Test.Utility;

/// <summary>
/// Integration tests for the StubBuilder pipeline. These tests require a GDK installation,
/// Visual Studio with C++ workload, and GXDK extensions for Xbox builds.
/// They are excluded from default test runs via [TestCategory("Integration")].
/// Run them explicitly by filtering on the Integration category in Test Explorer.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class StubBuildIntegrationTest
{
    private string _extractDir = string.Empty;
    private string _deployDir = string.Empty;
    private string _msbuildPath = string.Empty;
    private string _gdkVersion = string.Empty;
    private const string Platform = "Scarlett";
    private const string MsBuildPlatform = "Gaming.Xbox.Scarlett.x64";

    [TestInitialize]
    public void Setup()
    {
        _msbuildPath = StubBuilder.FindMsBuild();
        var versions = StubBuilder.EnumerateGdkVersions();
        _gdkVersion = versions.FirstOrDefault() ?? string.Empty;
        _deployDir = Path.Combine(Path.GetTempPath(), "StubBuildTest_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_deployDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (!string.IsNullOrEmpty(_extractDir) && Directory.Exists(_extractDir))
        {
            try { Directory.Delete(_extractDir, true); } catch { }
        }
        if (Directory.Exists(_deployDir))
        {
            try { Directory.Delete(_deployDir, true); } catch { }
        }
    }

    [TestMethod]
    public void ExtractStubProject_ProducesSlnFile()
    {
        // Act
        _extractDir = StubBuilder.ExtractStubProject();

        // Assert
        Assert.IsTrue(Directory.Exists(_extractDir), "Extraction directory should exist.");
        string slnPath = Path.Combine(_extractDir, "GDKStubExe.sln");
        Assert.IsTrue(File.Exists(slnPath), $"GDKStubExe.sln should exist at {slnPath}");
    }

    [TestMethod]
    public async Task BuildStubExe_Scarlett_Succeeds()
    {
        // Arrange
        if (string.IsNullOrEmpty(_msbuildPath))
            Assert.Inconclusive("MSBuild not found — Visual Studio with C++ workload required.");
        if (string.IsNullOrEmpty(_gdkVersion))
            Assert.Inconclusive("No GDK version installed.");

        string gdkRoot = StubBuilder.GetGdkRootPath();
        string gxdkPath = Path.Combine(gdkRoot, _gdkVersion, "GXDK");
        if (!Directory.Exists(gxdkPath))
            Assert.Inconclusive($"GXDK extensions not installed for GDK {_gdkVersion}.");

        _extractDir = StubBuilder.ExtractStubProject();
        string slnPath = Path.Combine(_extractDir, "GDKStubExe.sln");
        string arguments = StubBuilder.GetMsBuildArguments(slnPath, MsBuildPlatform, _gdkVersion);

        var buildOutput = new List<string>();

        // Act
        bool success = await StubBuilder.RunBuildAsync(_msbuildPath, arguments,
            line => buildOutput.Add(line));

        // Assert
        string fullOutput = string.Join(Environment.NewLine, buildOutput);
        Assert.IsTrue(success, $"Build should succeed. Output:\n{fullOutput}");

        string outputDir = StubBuilder.GetBuildOutputDir(_extractDir, MsBuildPlatform);
        string exePath = Path.Combine(outputDir, "GDKStubExe.exe");
        Assert.IsTrue(File.Exists(exePath), $"GDKStubExe.exe should exist at {exePath}");
    }

    [TestMethod]
    public async Task DeployStubFiles_Scarlett_CopiesRequiredFiles()
    {
        // Arrange — build first
        if (string.IsNullOrEmpty(_msbuildPath))
            Assert.Inconclusive("MSBuild not found — Visual Studio with C++ workload required.");
        if (string.IsNullOrEmpty(_gdkVersion))
            Assert.Inconclusive("No GDK version installed.");

        string gdkRoot = StubBuilder.GetGdkRootPath();
        string gxdkPath = Path.Combine(gdkRoot, _gdkVersion, "GXDK");
        if (!Directory.Exists(gxdkPath))
            Assert.Inconclusive($"GXDK extensions not installed for GDK {_gdkVersion}.");

        _extractDir = StubBuilder.ExtractStubProject();
        string slnPath = Path.Combine(_extractDir, "GDKStubExe.sln");
        string arguments = StubBuilder.GetMsBuildArguments(slnPath, MsBuildPlatform, _gdkVersion);

        bool buildOk = await StubBuilder.RunBuildAsync(_msbuildPath, arguments, _ => { });
        Assert.IsTrue(buildOk, "Build must succeed before deploy test.");

        string buildOutputDir = StubBuilder.GetBuildOutputDir(_extractDir, MsBuildPlatform);
        var deployOutput = new List<string>();

        // Act
        StubBuilder.DeployStubFiles(buildOutputDir, Platform, _gdkVersion, _deployDir,
            line => deployOutput.Add(line));

        // Assert — exe
        Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "GDKStubExe.exe")),
            "GDKStubExe.exe should be deployed.");

        // Assert — VC redist DLLs
        Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "vcruntime140.dll")),
            "vcruntime140.dll should be deployed.");
        Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "msvcp140.dll")),
            "msvcp140.dll should be deployed.");

        // Assert — Xbox DLLs
        string xboxBinDir = Path.Combine(gdkRoot, _gdkVersion, "xbox", "bin", "x64");
        if (File.Exists(Path.Combine(xboxBinDir, "XCurl.dll")))
        {
            Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "XCurl.dll")),
                "XCurl.dll should be deployed for console.");
        }

        // Assert — GameOS.Xvd
        string gameOsSrc = Path.Combine(gdkRoot, _gdkVersion, "GXDK", "sideload", "GameOS.Xvd");
        if (File.Exists(gameOsSrc))
        {
            Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "GameOS.Xvd")),
                "GameOS.Xvd should be deployed for console.");
        }
    }

    [TestMethod]
    public void UpdateGameConfig_Scarlett_AddsExecutableEntry()
    {
        // Arrange — create a minimal MGC
        string configPath = Path.Combine(_deployDir, "MicrosoftGame.config");
        File.WriteAllText(configPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <Game ConfigVersion="0">
                <Identity Name="TestGame" Publisher="TestPub" Version="1.0.0.0" />
            </Game>
            """);

        // Act
        StubBuilder.UpdateGameConfig(configPath, "GDKStubExe.exe", Platform);

        // Assert
        XmlDocument doc = new();
        doc.Load(configPath);
        var gameNode = doc.DocumentElement;
        Assert.IsNotNull(gameNode);

        var execList = gameNode["ExecutableList"];
        Assert.IsNotNull(execList, "ExecutableList should be created.");

        var exe = execList["Executable"];
        Assert.IsNotNull(exe, "Executable element should exist.");
        Assert.AreEqual("GDKStubExe.exe", exe.GetAttribute("Name"));
        Assert.AreEqual("Game", exe.GetAttribute("Id"));
        Assert.AreEqual(Platform, exe.GetAttribute("TargetDeviceFamily"));

        // Console should NOT have DesktopRegistration
        var desktopReg = gameNode["DesktopRegistration"];
        Assert.IsNull(desktopReg, "DesktopRegistration should not be added for console targets.");
    }

    [TestMethod]
    public async Task FullPipeline_Scarlett_ExtractBuildDeployUpdateConfig()
    {
        // Arrange
        if (string.IsNullOrEmpty(_msbuildPath))
            Assert.Inconclusive("MSBuild not found — Visual Studio with C++ workload required.");
        if (string.IsNullOrEmpty(_gdkVersion))
            Assert.Inconclusive("No GDK version installed.");

        string gdkRoot = StubBuilder.GetGdkRootPath();
        string gxdkPath = Path.Combine(gdkRoot, _gdkVersion, "GXDK");
        if (!Directory.Exists(gxdkPath))
            Assert.Inconclusive($"GXDK extensions not installed for GDK {_gdkVersion}.");

        // Create a minimal MGC in the deploy directory
        string configPath = Path.Combine(_deployDir, "MicrosoftGame.config");
        File.WriteAllText(configPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <Game ConfigVersion="0">
                <Identity Name="TestGame" Publisher="TestPub" Version="1.0.0.0" />
            </Game>
            """);

        var allOutput = new List<string>();
        Action<string> log = line => allOutput.Add(line);

        // Act — Extract
        _extractDir = StubBuilder.ExtractStubProject();
        string slnPath = Path.Combine(_extractDir, "GDKStubExe.sln");

        // Act — Build
        string arguments = StubBuilder.GetMsBuildArguments(slnPath, MsBuildPlatform, _gdkVersion);
        bool buildOk = await StubBuilder.RunBuildAsync(_msbuildPath, arguments, log);
        string fullOutput = string.Join(Environment.NewLine, allOutput);
        Assert.IsTrue(buildOk, $"Build should succeed. Output:\n{fullOutput}");

        // Act — Deploy
        string buildOutputDir = StubBuilder.GetBuildOutputDir(_extractDir, MsBuildPlatform);
        StubBuilder.DeployStubFiles(buildOutputDir, Platform, _gdkVersion, _deployDir, log);

        // Act — Update config
        StubBuilder.UpdateGameConfig(configPath, "GDKStubExe.exe", Platform);

        // Assert — exe deployed
        Assert.IsTrue(File.Exists(Path.Combine(_deployDir, "GDKStubExe.exe")),
            "GDKStubExe.exe should exist in deploy dir.");

        // Assert — config updated
        XmlDocument doc = new();
        doc.Load(configPath);
        var execList = doc.DocumentElement?["ExecutableList"];
        Assert.IsNotNull(execList, "ExecutableList should exist in config.");
        var exe = execList["Executable"];
        Assert.IsNotNull(exe, "Executable entry should exist.");
        Assert.AreEqual("GDKStubExe.exe", exe.GetAttribute("Name"));
        Assert.AreEqual(Platform, exe.GetAttribute("TargetDeviceFamily"));
    }
}
