namespace PackageUploader.UI.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

[TestClass]
public class XvcXtsEntryTest
{
    private MemoryStream _testStream;
    private XvcXtsEntry _entry;
    private uint _testPageOffset;
    private uint _testXtsOffset;

    [TestInitialize]
    public void Setup()
    {
        _testStream = new MemoryStream();
        _testPageOffset = 12345;
        _testXtsOffset = 67890;

        using (BinaryWriter writer = new BinaryWriter(_testStream, Encoding.Unicode, true))
        {
            writer.Write(_testPageOffset);
            writer.Write(_testXtsOffset);
        }

        _testStream.Position = 0;
        var entries = XvcXtsEntry.Read(_testStream, 1);
        _entry = entries[0];
    }

    [TestCleanup]
    public void Cleanup()
    {
        _testStream.Dispose();
    }

    [TestMethod]
    public void XvcXtsEntry_ReadTest_SingleEntry()
    {
        // Assert
        Assert.AreEqual(_testPageOffset, _entry.PageOffset, "PageOffset property doesn't match expected value");
        Assert.AreEqual(_testXtsOffset, _entry.XtsOffset, "XtsOffset property doesn't match expected value");
    }

    [TestMethod]
    public void XvcXtsEntry_ReadTest_MultipleEntries()
    {
        // Arrange
        uint[] pageOffsets = new uint[] { 100, 200, 300 };
        uint[] xtsOffsets = new uint[] { 1000, 2000, 3000 };
        
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true))
        {
            for (int i = 0; i < pageOffsets.Length; i++)
            {
                writer.Write(pageOffsets[i]);
                writer.Write(xtsOffsets[i]);
            }

            stream.Position = 0;
            
            // Act
            var entries = XvcXtsEntry.Read(stream, (uint)pageOffsets.Length);
            
            // Assert
            Assert.AreEqual(pageOffsets.Length, entries.Length, "Number of entries read doesn't match expected count");
            
            for (int i = 0; i < pageOffsets.Length; i++)
            {
                Assert.AreEqual(pageOffsets[i], entries[i].PageOffset, $"PageOffset for entry {i} doesn't match expected value");
                Assert.AreEqual(xtsOffsets[i], entries[i].XtsOffset, $"XtsOffset for entry {i} doesn't match expected value");
            }
        }
    }

    [TestMethod]
    public void XvcXtsEntry_ReadTest_ZeroEntries()
    {
        // Arrange
        using (MemoryStream stream = new MemoryStream())
        {
            // Act
            var entries = XvcXtsEntry.Read(stream, 0);
            
            // Assert
            Assert.AreEqual(0, entries.Length, "Should return an empty array for zero entries");
        }
    }
}
