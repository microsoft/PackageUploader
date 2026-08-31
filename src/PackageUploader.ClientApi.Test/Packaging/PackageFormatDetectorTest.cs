// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Packaging;

namespace PackageUploader.ClientApi.Test.Packaging;

[TestClass]
public class PackageFormatDetectorTest
{
    private const int MinimumDetectableSize = 4096;

    private static string CreatePackage(string extension, byte[] header = null, byte[] footer = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"pu-detector-{Guid.NewGuid():N}{extension}");
        var contents = new byte[MinimumDetectableSize * 2];

        if (header is not null)
        {
            Array.Copy(header, contents, header.Length);
        }

        if (footer is not null)
        {
            Array.Copy(footer, 0, contents, contents.Length - footer.Length, footer.Length);
        }

        File.WriteAllBytes(path, contents);
        return path;
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_ZipLocalFileHeader_ReturnsTrue()
    {
        var path = CreatePackage(".msixvc", header: [0x50, 0x4B, 0x03, 0x04]);
        try
        {
            Assert.IsTrue(PackageFormatDetector.IsLikelyMsixvc2Package(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_ZipEndOfCentralDirectorySignature_ReturnsTrue()
    {
        var path = CreatePackage(".msixvc", header: [0x01, 0x02, 0x03, 0x04], footer: [0x50, 0x4B, 0x05, 0x06]);
        try
        {
            Assert.IsTrue(PackageFormatDetector.IsLikelyMsixvc2Package(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_NoZipSignatures_ReturnsFalse()
    {
        var path = CreatePackage(".msixvc", header: [0x01, 0x02, 0x03, 0x04]);
        try
        {
            Assert.IsFalse(PackageFormatDetector.IsLikelyMsixvc2Package(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_WrongExtension_ReturnsFalse()
    {
        var path = CreatePackage(".xvc", header: [0x50, 0x4B, 0x03, 0x04]);
        try
        {
            Assert.IsFalse(PackageFormatDetector.IsLikelyMsixvc2Package(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_FileTooSmall_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pu-detector-{Guid.NewGuid():N}.msixvc");
        File.WriteAllBytes(path, [0x50, 0x4B, 0x03, 0x04]);
        try
        {
            Assert.IsFalse(PackageFormatDetector.IsLikelyMsixvc2Package(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLikelyMsixvc2Package_MissingFile_ReturnsFalse() =>
        Assert.IsFalse(PackageFormatDetector.IsLikelyMsixvc2Package(@"C:\does\not\exist.msixvc"));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void IsLikelyMsixvc2Package_EmptyPath_ReturnsFalse(string path) =>
        Assert.IsFalse(PackageFormatDetector.IsLikelyMsixvc2Package(path));

    #region Loose game content

    /// <summary>
    /// A content directory is not a package. Recognising it lets the caller be told that PackageUploader has
    /// no packaging step, instead of being told the file was not found.
    /// </summary>
    [TestMethod]
    public void IsLooseGameContent_DirectoryWithGameConfig_ReturnsTrue()
    {
        var directory = CreateLooseContent();
        try
        {
            Assert.IsTrue(PackageFormatDetector.IsLooseGameContent(directory));
            Assert.AreEqual(
                Path.Combine(directory, "MicrosoftGame.config"),
                PackageFormatDetector.FindGameConfig(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void IsLooseGameContent_GameConfigFile_ReturnsTrue()
    {
        var directory = CreateLooseContent();
        var gameConfig = Path.Combine(directory, "MicrosoftGame.config");
        try
        {
            Assert.IsTrue(PackageFormatDetector.IsLooseGameContent(gameConfig));
            Assert.AreEqual(gameConfig, PackageFormatDetector.FindGameConfig(gameConfig));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// The guard must not fire for a real package, or every supported upload would be refused.
    /// </summary>
    [TestMethod]
    public void IsLooseGameContent_PackageFile_ReturnsFalse()
    {
        var path = CreatePackage(".msixvc", header: [0x50, 0x4B, 0x03, 0x04]);
        try
        {
            Assert.IsFalse(PackageFormatDetector.IsLooseGameContent(path));
            Assert.IsNull(PackageFormatDetector.FindGameConfig(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IsLooseGameContent_NullOrMissingPath_ReturnsFalse()
    {
        Assert.IsFalse(PackageFormatDetector.IsLooseGameContent(null));
        Assert.IsFalse(PackageFormatDetector.IsLooseGameContent("   "));
        Assert.IsFalse(PackageFormatDetector.IsLooseGameContent(Path.Combine(Path.GetTempPath(), $"pu-missing-{Guid.NewGuid():N}")));
    }

    /// <summary>
    /// A directory with no MicrosoftGame.config is still not a package file, so it is still refused - just
    /// without the config path in the message.
    /// </summary>
    [TestMethod]
    public void IsLooseGameContent_DirectoryWithoutGameConfig_ReturnsTrueWithNoConfigPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pu-loose-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Assert.IsTrue(PackageFormatDetector.IsLooseGameContent(directory));
            Assert.IsNull(PackageFormatDetector.FindGameConfig(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateLooseContent()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pu-loose-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "MicrosoftGame.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Game configVersion=\"1\" />");
        return directory;
    }

    #endregion
}
