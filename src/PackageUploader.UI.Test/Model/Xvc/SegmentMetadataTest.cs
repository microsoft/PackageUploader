 using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Test.Model
{
    /// Test suite for the SegmentMetadata class that validates reading and parsing 
    /// of XVC segment metadata from binary data.
    [TestClass]
    public class SegmentMetadataTest
    {
        // Constants defining the binary structure of segment metadata
        private const int SIZE_OF_HEADER = 100;        // Total header size in bytes
        private const int SIZE_OF_SEGMENT = 16;        // Size of each segment entry in bytes
        
        // Test data constants for header validation
        private const uint EXPECTED_MAGIC = 0x12345678;          // Magic number to identify valid metadata
        private const uint EXPECTED_VERSION_MAJOR = 1;           // Major version number
        private const uint EXPECTED_VERSION_MINOR = 0;           // Minor version number  
        private const uint EXPECTED_FLAGS = 42;                  // Test flags value
        private static readonly Guid EXPECTED_PDUID = new Guid("00112233-4455-6677-8899-AABBCCDDEEFF"); // Product ID

        /// Tests that reading segment metadata with a single segment correctly parses all header fields
        /// and returns the expected values for magic number, version, sizes, GUID, and flags.
        [TestMethod]
        public void Read_WithValidSingleSegment_ReturnsCorrectMetadata()
        {
            // Arrange - Create test data with exactly one segment
            var bytes = CreateTestMetadataBytes(1);

            // Act - Parse the binary data into a SegmentMetadata object
            var metadata = SegmentMetadata.Read(bytes);

            // Assert - Verify all header fields are correctly parsed
            Assert.AreEqual(EXPECTED_MAGIC, metadata.Magic);
            Assert.AreEqual(EXPECTED_VERSION_MAJOR, metadata.VersionMajor);
            Assert.AreEqual(EXPECTED_VERSION_MINOR, metadata.VersionMinor);
            Assert.AreEqual((uint)SIZE_OF_HEADER, metadata.HeaderSize);
            Assert.AreEqual(1, metadata.SegmentCount);
            Assert.AreEqual(EXPECTED_PDUID, metadata.PDUID);
            Assert.AreEqual(EXPECTED_FLAGS, metadata.Flags);
        }

        /// Tests that metadata with multiple segments correctly parses segment count
        /// and can retrieve the correct path for each segment.
        [TestMethod]
        public void Read_WithMultipleSegments_ReturnsCorrectMetadata()
        {
            // Arrange - Create test data with three segments
            var bytes = CreateTestMetadataBytes(3);

            // Act - Parse the binary data into a SegmentMetadata object
            var metadata = SegmentMetadata.Read(bytes);

            // Assert - Verify segment count and path retrieval for all segments
            Assert.AreEqual(3, metadata.SegmentCount);
            Assert.AreEqual("C:\\Test\\File1.txt", metadata.GetPath(0));
            Assert.AreEqual("C:\\Test\\File2.txt", metadata.GetPath(1));
            Assert.AreEqual("C:\\Test\\File3.txt", metadata.GetPath(2));
        }

        /// Tests that the GetPath method correctly retrieves file paths for valid segment indices.
        [TestMethod]
        public void GetPath_ReturnsCorrectPath()
        {
            // Arrange - Create metadata with two segments
            var bytes = CreateTestMetadataBytes(2);
            var metadata = SegmentMetadata.Read(bytes);

            // Act & Assert - Verify each segment returns the expected file path
            Assert.AreEqual("C:\\Test\\File1.txt", metadata.GetPath(0));
            Assert.AreEqual("C:\\Test\\File2.txt", metadata.GetPath(1));
        }

        /// Tests that the GetSize method correctly retrieves file sizes for valid segment indices.
        /// Each segment has a size that increases by 1024 bytes per segment index.
        [TestMethod]
        public void GetSize_ReturnsCorrectSize()
        {
            // Arrange - Create metadata with two segments
            var bytes = CreateTestMetadataBytes(2);
            var metadata = SegmentMetadata.Read(bytes);

            // Act & Assert - Verify each segment returns the expected file size
            // Test data creates sizes as (index + 1) * 1024 bytes
            Assert.AreEqual((ulong)1024, metadata.GetSize(0));  // First segment: 1 * 1024
            Assert.AreEqual((ulong)2048, metadata.GetSize(1));  // Second segment: 2 * 1024
        }

        /// Tests that GetPath throws IndexOutOfRangeException when accessing an invalid segment index.
        /// This validates proper boundary checking in the GetPath method.
        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void GetPath_WithInvalidIndex_ThrowsException()
        {
            // Arrange - Create metadata with only one segment (index 0 valid)
            var bytes = CreateTestMetadataBytes(1);
            var metadata = SegmentMetadata.Read(bytes);

            // Act - Attempt to access segment index 1, which should throw
            metadata.GetPath(1);
        }

        /// Tests that GetSize throws IndexOutOfRangeException when accessing an invalid segment index.
        /// This validates proper boundary checking in the GetSize method.

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void GetSize_WithInvalidIndex_ThrowsException()
        {
            // Arrange - Create metadata with only one segment (index 0 valid)
            var bytes = CreateTestMetadataBytes(1);
            var metadata = SegmentMetadata.Read(bytes);

            // Act - Attempt to access segment index 1, which should throw
            metadata.GetSize(1);
        }
        
        /// Helper method that creates test segment metadata binary data with the specified number of segments.
        /// This method constructs the binary structure that matches the SegmentMetadata.Read() expectations:
        /// - Header (100 bytes): Magic, versions, sizes, GUID, flags, and padding
        /// - Segment entries (16 bytes each): Flags, path length, path offset, and file size
        /// - Path data block: Unicode-encoded file paths without padding
        private byte[] CreateTestMetadataBytes(int segmentCount)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Pre-calculate test paths and total path data size for accurate header values
                string[] testPaths = new string[segmentCount];
                uint totalPathDataSize = 0;
                for (int i = 0; i < segmentCount; i++)
                {
                    testPaths[i] = $"C:\\Test\\File{i + 1}.txt";
                    totalPathDataSize += (uint)(testPaths[i].Length * 2); // Unicode = 2 bytes per char
                }

                // Write the 100-byte header section
                writer.Write(EXPECTED_MAGIC);         // Magic number (4 bytes)
                writer.Write(EXPECTED_VERSION_MAJOR); // Major version (4 bytes)
                writer.Write(EXPECTED_VERSION_MINOR); // Minor version (4 bytes)
                writer.Write((uint)SIZE_OF_HEADER);   // Header size = 100 (4 bytes)
                writer.Write(segmentCount);           // Number of segments (4 bytes)
                writer.Write(totalPathDataSize);      // Total size of path data in bytes (4 bytes)
                writer.Write(EXPECTED_PDUID.ToByteArray()); // Product GUID (16 bytes)
                writer.Write(EXPECTED_FLAGS);         // Flags value (4 bytes)

                // Write remaining 56 bytes of padding to reach 100-byte header size
                // Total so far: 4+4+4+4+4+4+16+4 = 44 bytes, need 56 more for 100 total
                writer.Write(new byte[56]);

                // Write segment entries (16 bytes each)
                int currentPathOffset = 0;
                for (int i = 0; i < segmentCount; i++)
                {
                    string path = testPaths[i];

                    writer.Write((ushort)0);              // Segment flags (2 bytes)
                    writer.Write((ushort)path.Length);    // Path length in characters (2 bytes)
                    writer.Write(currentPathOffset);      // Offset into path data block (4 bytes)
                    writer.Write((ulong)((i + 1) * 1024)); // File size: 1024, 2048, 3072, etc. (8 bytes)

                    currentPathOffset += path.Length * 2; // Track offset for next path (Unicode = 2 bytes/char)
                }

                // Write path data block - exact-length Unicode strings without padding
                // This section immediately follows the segment entries
                for (int i = 0; i < segmentCount; i++)
                {
                    string path = testPaths[i];
                    byte[] pathBytes = Encoding.Unicode.GetBytes(path);
                    writer.Write(pathBytes);
                }

                return ms.ToArray();
            }
        }
    }
}