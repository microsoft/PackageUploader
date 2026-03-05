// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;

namespace PackageUploader.UI.Utility;

/// <summary>
/// Provides methods for extracting, building, and deploying the GDK stub executable.
/// </summary>
public static class StubBuilder
{
    private const string GdkDefaultRoot = @"C:\Program Files (x86)\Microsoft GDK";
    private const string VsWherePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";

    /// <summary>
    /// Enumerates installed GDK version directories (e.g., "240602", "251001").
    /// Returns versions in descending order (latest first).
    /// </summary>
    public static List<string> EnumerateGdkVersions()
    {
        string gdkRoot = GetGdkRootPath();
        if (string.IsNullOrEmpty(gdkRoot) || !Directory.Exists(gdkRoot))
            return [];

        return Directory.GetDirectories(gdkRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name) && Regex.IsMatch(name!, @"^\d{6}$"))
            .OrderDescending()
            .ToList()!;
    }

    /// <summary>
    /// Gets the GDK root installation path (e.g., "C:\Program Files (x86)\Microsoft GDK").
    /// </summary>
    public static string GetGdkRootPath()
    {
        string? gdkPath = Environment.GetEnvironmentVariable("GameDK");
        if (!string.IsNullOrEmpty(gdkPath) && Directory.Exists(gdkPath))
            return gdkPath.TrimEnd('\\');

        string[] registryPaths =
        [
            @"SOFTWARE\Microsoft\GDK\Installed Roots",
            @"SOFTWARE\WOW6432Node\Microsoft\GDK\Installed Roots"
        ];
        foreach (var regPath in registryPaths)
        {
            using var key = Registry.LocalMachine.OpenSubKey(regPath);
            string? installPath = key?.GetValue("GDKInstallPath") as string;
            if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                return installPath.TrimEnd('\\');
        }

        if (Directory.Exists(GdkDefaultRoot))
            return GdkDefaultRoot;

        return string.Empty;
    }

    /// <summary>
    /// Finds MSBuild.exe using vswhere.exe. Returns empty string if not found.
    /// </summary>
    public static string FindMsBuild()
    {
        return RunVsWhere("-latest -requires Microsoft.Component.MSBuild -find \"MSBuild\\**\\Bin\\MSBuild.exe\"");
    }

    /// <summary>
    /// Finds the VC143 CRT redist directory (contains vcruntime140.dll, msvcp140.dll, etc.).
    /// </summary>
    public static string FindVcRedistCrtDir()
    {
        string dllPath = RunVsWhere(
            "-latest -find \"VC\\Redist\\MSVC\\*\\x64\\Microsoft.VC143.CRT\\vcruntime140.dll\"");
        if (!string.IsNullOrEmpty(dllPath))
            return Path.GetDirectoryName(dllPath) ?? string.Empty;
        return string.Empty;
    }

    private static string RunVsWhere(string arguments)
    {
        if (!File.Exists(VsWherePath))
            return string.Empty;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = VsWherePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string path = line.Trim();
                if (File.Exists(path) || Directory.Exists(path))
                    return path;
            }
        }
        catch
        {
            // vswhere not available or failed
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts the embedded GDKStubExe.zip to a build directory under %APPDATA%.
    /// Uses a non-temp location to avoid MSB8029 warnings about intermediate/output dirs in %TEMP%.
    /// </summary>
    public static string ExtractStubProject()
    {
        var assembly = GDKStubExe.GDKStubExeResources.Assembly;
        using var stream = assembly.GetManifestResourceStream("GDKStubExe.zip")
            ?? throw new InvalidOperationException("Embedded GDKStubExe.zip resource not found.");

        string baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XboxPackageTool", "StubBuild_" + Path.GetRandomFileName());
        Directory.CreateDirectory(baseDir);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(baseDir);

        return baseDir;
    }

    /// <summary>
    /// Maps TargetDeviceFamily to MSBuild platform string.
    /// </summary>
    public static string GetMsBuildPlatform(string targetDeviceFamily)
    {
        return targetDeviceFamily switch
        {
            "PC" => "x64",
            "Scarlett" => "Gaming.Xbox.Scarlett.x64",
            "XboxOne" => "Gaming.Xbox.XboxOne.x64",
            _ => throw new ArgumentException($"Unknown target device family: {targetDeviceFamily}")
        };
    }

    /// <summary>
    /// Builds the MSBuild arguments for the stub exe build.
    /// </summary>
    public static string GetMsBuildArguments(string slnPath, string platform, string gdkVersion)
    {
        string gdkRoot = GetGdkRootPath();
        string gdkVersionPath = Path.Combine(gdkRoot, gdkVersion);

        var args = $"\"{slnPath}\" /p:Configuration=Release /p:Platform=\"{platform}\" /nologo /v:normal";

        if (platform == "x64")
        {
            string grdkPath = Path.Combine(gdkVersionPath, "GRDK") + "/";
            args += $" /p:GRDKLatest=\"{grdkPath}\"";
        }
        else
        {
            // Trailing backslash before closing quote is interpreted as escaped quote by
            // the Windows command-line parser. Use forward slash as path terminator instead.
            args += $" /p:GameDK=\"{gdkRoot}/\"";
            args += $" /p:GDKCrossPlatformPath=\"{gdkVersionPath}/\"";
            args += $" /p:GameDKCoreLatest=\"{gdkVersionPath}/\"";
        }

        return args;
    }

    /// <summary>
    /// Gets the expected build output directory for the given platform.
    /// </summary>
    public static string GetBuildOutputDir(string projectDir, string platform)
    {
        return Path.Combine(projectDir, platform, "Release");
    }

    /// <summary>
    /// Runs the MSBuild process, streaming output via callback. Returns true on success.
    /// </summary>
    public static async Task<bool> RunBuildAsync(string msbuildPath, string arguments,
        Action<string> outputCallback, CancellationToken cancellationToken = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = msbuildPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputCallback(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) outputCallback(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode == 0;
    }

    /// <summary>
    /// Deploys the built stub exe and required dependencies to the target directory.
    /// </summary>
    public static void DeployStubFiles(string buildOutputDir, string targetDeviceFamily,
        string gdkVersion, string targetDir, Action<string> outputCallback)
    {
        string exePath = Path.Combine(buildOutputDir, "GDKStubExe.exe");
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Built executable not found at: {exePath}");

        string destExe = Path.Combine(targetDir, "GDKStubExe.exe");
        File.Copy(exePath, destExe, overwrite: true);
        outputCallback($"Copied GDKStubExe.exe → {targetDir}");

        // VC143 CRT DLLs required for all platforms
        string vcRedistDir = FindVcRedistCrtDir();
        if (!string.IsNullOrEmpty(vcRedistDir) && Directory.Exists(vcRedistDir))
        {
            foreach (var dllPath in Directory.GetFiles(vcRedistDir, "*.dll"))
            {
                string dllName = Path.GetFileName(dllPath);
                File.Copy(dllPath, Path.Combine(targetDir, dllName), overwrite: true);
                outputCallback($"Copied {dllName} → {targetDir}");
            }
        }
        else
        {
            outputCallback("WARNING: VC143 CRT redist directory not found. The stub exe may not run without vcruntime140.dll and msvcp140.dll.");
        }

        if (targetDeviceFamily is "Scarlett" or "XboxOne")
        {
            string gdkRoot = GetGdkRootPath();
            string gdkVersionPath = Path.Combine(gdkRoot, gdkVersion);

            // Xbox cross-platform DLLs
            string xboxBinDir = Path.Combine(gdkVersionPath, "xbox", "bin", "x64");
            foreach (var dll in new[] { "libHttpClient.dll", "XCurl.dll" })
            {
                string srcDll = Path.Combine(xboxBinDir, dll);
                if (File.Exists(srcDll))
                {
                    File.Copy(srcDll, Path.Combine(targetDir, dll), overwrite: true);
                    outputCallback($"Copied {dll} → {targetDir}");
                }
                else
                {
                    outputCallback($"WARNING: {dll} not found at {srcDll}");
                }
            }

            // GameOS.Xvd for console sideloading
            string gameOsPath = Path.Combine(gdkVersionPath, "GXDK", "sideload", "GameOS.Xvd");
            if (File.Exists(gameOsPath))
            {
                File.Copy(gameOsPath, Path.Combine(targetDir, "GameOS.Xvd"), overwrite: true);
                outputCallback($"Copied GameOS.Xvd → {targetDir}");
            }
            else
            {
                outputCallback($"WARNING: GameOS.Xvd not found at {gameOsPath}");
            }
        }
    }

    /// <summary>
    /// Adds an Executable entry to the MicrosoftGame.config file.
    /// </summary>
    public static void UpdateGameConfig(string configPath, string exeName, string targetDeviceFamily)
    {
        XmlDocument doc = new();
        doc.Load(configPath);

        var gameNode = doc.DocumentElement
            ?? throw new InvalidOperationException("Invalid MicrosoftGame.config: no root element.");

        // Find or create ExecutableList
        var execList = gameNode["ExecutableList"];
        if (execList == null)
        {
            execList = doc.CreateElement("ExecutableList");
            gameNode.AppendChild(execList);
        }

        var exeElement = doc.CreateElement("Executable");
        exeElement.SetAttribute("Name", exeName);
        exeElement.SetAttribute("Id", "Game");
        exeElement.SetAttribute("TargetDeviceFamily", targetDeviceFamily);
        execList.AppendChild(exeElement);

        // PC packages require a VC14 KnownDependency under DesktopRegistration
        if (targetDeviceFamily == "PC")
        {
            var desktopReg = gameNode["DesktopRegistration"];
            if (desktopReg == null)
            {
                desktopReg = doc.CreateElement("DesktopRegistration");
                gameNode.AppendChild(desktopReg);
            }

            var depList = desktopReg["DependencyList"];
            if (depList == null)
            {
                depList = doc.CreateElement("DependencyList");
                desktopReg.AppendChild(depList);
            }

            // Only add if not already present
            bool hasVc14 = false;
            foreach (XmlNode child in depList.ChildNodes)
            {
                if (child is XmlElement el && el.GetAttribute("Name") == "VC14")
                {
                    hasVc14 = true;
                    break;
                }
            }
            if (!hasVc14)
            {
                var knownDep = doc.CreateElement("KnownDependency");
                knownDep.SetAttribute("Name", "VC14");
                depList.AppendChild(knownDep);
            }
        }

        var writerSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = false
        };
        using var writer = XmlWriter.Create(configPath, writerSettings);
        doc.Save(writer);
    }
}
