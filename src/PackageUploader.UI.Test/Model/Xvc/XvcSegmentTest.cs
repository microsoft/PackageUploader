namespace PackageUploader.UI.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

[TestClass]
public class XvcSegmentTest
{
    private MemoryStream _testStream;
    private XvcSegment _segment;
    private uint _testPageOffset;
    private ulong _testHash;

    [TestInitialize]
    public void Setup()
    {
        _testStream = new MemoryStream();
        _testPageOffset = 12345;
        _testHash = 0x1234567890ABCDEF;

        using (BinaryWriter writer = new BinaryWriter(_testStream, Encoding.Unicode, true))
        {
            writer.Write(_testPageOffset);
            writer.Write(_testHash);
        }

        _testStream.Position = 0;
        var segments = XvcSegment.Read(_testStream, 1);
        _segment = segments[0];
    }

    [TestCleanup]
    public void Cleanup()
    {
        _testStream.Dispose();
    }

    [TestMethod]
    public void XvcSegment_ReadTest_SingleSegment()
    {
        // Assert
        Assert.AreEqual(_testPageOffset, _segment.PageOffset, "PageOffset property doesn't match expected value");
        Assert.AreEqual(_testHash, _segment.Hash, "Hash property doesn't match expected value");
    }

    [TestMethod]
    public void XvcSegment_ReadTest_MultipleSegments()
    {
        // Arrange
        uint[] pageOffsets = new uint[] { 100, 200, 300 };
        ulong[] hashes = new ulong[] { 0x1111111111111111, 0x2222222222222222, 0x3333333333333333 };
        
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true))
        {
            for (int i = 0; i < pageOffsets.Length; i++)
            {
                writer.Write(pageOffsets[i]);
                writer.Write(hashes[i]);
            }

            stream.Position = 0;
            
            // Act
            var segments = XvcSegment.Read(stream, (uint)pageOffsets.Length);
            
            // Assert
            Assert.AreEqual(pageOffsets.Length, segments.Length, "Number of segments read doesn't match expected count");
            
            for (int i = 0; i < pageOffsets.Length; i++)
            {
                Assert.AreEqual(pageOffsets[i], segments[i].PageOffset, $"PageOffset for segment {i} doesn't match expected value");
                Assert.AreEqual(hashes[i], segments[i].Hash, $"Hash for segment {i} doesn't match expected value");
            }
        }
    }

    [TestMethod]
    public void XvcSegment_ReadTest_ZeroSegments()
    {
        // Arrange
        using (MemoryStream stream = new MemoryStream())
        {
            // Act
            var segments = XvcSegment.Read(stream, 0);
            
            // Assert
            Assert.AreEqual(0, segments.Length, "Should return an empty array for zero segments");
        }
    }
}
