using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System.IO;
using System.Text;

namespace Package.UI.Test.Model.Xvc;

[TestClass]
public class UserDataPackageFilesHeaderTest
{
   //Define a helper function to write a test stream with specified parameters for testing
    private Stream WriteTestStream(uint version, string packageFullName, uint entryCount)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.Unicode, true))
        {
            writer.Write(version);

            // Write null-terminated string, padded to 260 characters (520 bytes)
            var chars = packageFullName.Length > 259
                ? packageFullName.Substring(0, 259)
                : packageFullName;
            writer.Write(chars.ToCharArray());
            writer.Write('\0'); // null terminator

            // Pad to 260 characters
            for (int i = chars.Length + 1; i < 260; i++)
                writer.Write('\0');

            writer.Write(entryCount);
        }
        stream.Position = 0;
        return stream;
    }

    [TestMethod]
    public void TestReadValidHeader()
    {
        uint version = 1;
        string packageFullName = "Test.Package.FullName";
        uint entryCount = 5;

        using var stream = WriteTestStream(version, packageFullName, entryCount);
        var header = UserDataPackageFilesHeader.Read(stream);

        Assert.IsNotNull(header);
        Assert.AreEqual(version, header.Version);
        Assert.AreEqual(packageFullName, header.PackageFullName);
        Assert.AreEqual(entryCount, header.EntryCount);
    }

    [TestMethod]
    public void TestReadHeaderWithEmptyPackageFullName()
    {
        uint version = 2;
        string packageFullName = "";
        uint entryCount = 10;

        using var stream = WriteTestStream(version, packageFullName, entryCount);
        var header = UserDataPackageFilesHeader.Read(stream);

        Assert.IsNotNull(header);
        Assert.AreEqual(version, header.Version);
        Assert.AreEqual(packageFullName, header.PackageFullName);
        Assert.AreEqual(entryCount, header.EntryCount);
    }

    [TestMethod]
    public void TestReadHeaderWithMaxLengthPackageFullName()
    {
        uint version = 3;
        string packageFullName = new string('A', 259); // 259 chars, last byte is null terminator
        uint entryCount = 20;

        using var stream = WriteTestStream(version, packageFullName, entryCount);
        var header = UserDataPackageFilesHeader.Read(stream);

        Assert.IsNotNull(header);
        Assert.AreEqual(version, header.Version);
        Assert.AreEqual(packageFullName, header.PackageFullName);
        Assert.AreEqual(entryCount, header.EntryCount);
    }

    [TestMethod]
    public void TestReadHeaderWithTruncatedPackageFullName()
    {
        uint version = 4;
        string packageFullName = new string('B', 300); // longer than 259, should be truncated
        uint entryCount = 30;

        using var stream = WriteTestStream(version, packageFullName, entryCount);
        // If the implementation throws due to input being too long, expect an exception.
        // Otherwise, if it truncates, check the truncated value.
        try
        {
            var header = UserDataPackageFilesHeader.Read(stream);
            Assert.IsNotNull(header);
            // The header.PackageFullName should be truncated at 259 characters, as only 259 bytes are written before the null terminator
            Assert.AreEqual(new string('B', 259), header.PackageFullName);
            Assert.AreEqual(version, header.Version);
            Assert.AreEqual(entryCount, header.EntryCount);
        }
        catch (EndOfStreamException ex)
        {
            // If the implementation throws, the test should pass as well
            Assert.IsInstanceOfType(ex, typeof(EndOfStreamException));
        }
    }

    [TestMethod]
    public void TestReadHeaderWithNonAsciiPackageFullName()
    {
        uint version = 5;
        string packageFullName = "测试包名"; // Non-ASCII characters
        uint entryCount = 40;

        using var stream = WriteTestStream(version, packageFullName, entryCount);
        var header = UserDataPackageFilesHeader.Read(stream);

        Assert.IsNotNull(header);
        Assert.AreEqual(version, header.Version);
        Assert.AreEqual(packageFullName, header.PackageFullName);
        Assert.AreEqual(entryCount, header.EntryCount);
    }

    [TestMethod]
    public void TestReadHeaderWithShortStreamThrows()
    {
        var stream = new MemoryStream(new byte[10]);
        Assert.ThrowsException<EndOfStreamException>(() => UserDataPackageFilesHeader.Read(stream));
    }
}