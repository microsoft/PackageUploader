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
}
