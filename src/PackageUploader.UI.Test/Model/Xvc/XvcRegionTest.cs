using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Test.Model
{
    [TestClass]
    public class XvcRegionTest
    {
        [TestMethod]
        public void Read_ValidStream_ReturnsCorrectObject()
        {
            // Arrange
            // Populate test data for XvcRegion
            var testData = CreateXvcRegionTestData(
                id: 0x40000001,
                keyIndex: 0x1234,
                spare0: 0x5678,
                flags: XvcRegionFlags.XVC_REGION_FLAG_SYSTEM_METADATA,
                firstSegmentIndex: 42,
                description: "Test Description",
                offset: 0x1000,
                length: 0x2000,
                hash: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            );

            using (var stream = new MemoryStream(testData))
            {
                // Act
                var region = XvcRegion.Read(stream);

                // Assert
                // Checking if each field is correctly read based on the test data
                Assert.AreEqual((uint)0x40000001, region.Id);
                Assert.AreEqual((ushort)0x1234, region.KeyIndex);
                Assert.AreEqual((ushort)0x5678, region.Spare0);
                Assert.AreEqual(XvcRegionFlags.XVC_REGION_FLAG_SYSTEM_METADATA, region.Flags);
                Assert.AreEqual((uint)42, region.FirstSegmentIndex);
                Assert.IsTrue(region.Description.StartsWith("Test Description"));
                Assert.AreEqual((ulong)0x1000, region.Offset);
                Assert.AreEqual((ulong)0x2000, region.Length);
                Assert.AreEqual(8, region.Hash.Length);
                Assert.AreEqual(1, region.Hash[0]);
                Assert.AreEqual(8, region.Hash[7]);
            }
        }

        [TestMethod]
        public void IsHashSame_SameHashes_ReturnsTrue()
        {
            // Arrange
            // Comparing hashes to make sure that they are equal
            var region1 = CreateXvcRegion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            var region2 = CreateXvcRegion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            // Act
            var result = region1.IsHashSame(region2);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsHashSame_DifferentHashes_ReturnsFalse()
        {
            // Arrange
            // Comparing hashes to make sure that they are different
            var region1 = CreateXvcRegion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            var region2 = CreateXvcRegion(new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 });

            // Act
            var result = region1.IsHashSame(region2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsSystem_WithSystemId_ReturnsTrue()
        {
            // Arrange
            // Making sure that the region is recognized as a system region
            var region = new XvcRegion { Id = 0x40000001 };

            // Act & Assert
            Assert.IsTrue(region.IsSystem);
        }

        [TestMethod]
        public void IsSystem_WithNonSystemId_ReturnsFalse()
        {
            // Arrange
            // Making sure that the region is NOT recognized as a system region
            var region = new XvcRegion { Id = 0x10000000 };

            // Act & Assert
            Assert.IsFalse(region.IsSystem);
        }

        [TestMethod]
        public void IsOnDemand_WithOnDemandFlag_ReturnsTrue()
        {
            // Arrange
            // Making sure that the IsOnDemandFlag is set to true
            var region = new XvcRegion { Flags = XvcRegionFlags.XVC_REGION_FLAG_ON_DEMAND };

            // Act & Assert
            Assert.IsTrue(region.IsOnDemand);
        }

        [TestMethod]
        public void IsOnDemand_WithoutOnDemandFlag_ReturnsFalse()
        {
            // Arrange
            // Making sure that the IsOnDemandFlag is set to false
            var region = new XvcRegion { Flags = XvcRegionFlags.XVC_REGION_FLAG_PREVIEW };

            // Act & Assert
            Assert.IsFalse(region.IsOnDemand);
        }

        [TestMethod]
        public void IsHashed_WithNonHashedId_ReturnsFalse()
        {
            // Arrange
            // Making sure that the region is not recognized as a region that is hashed with a non hashed id
            uint[] nonHashedIds =
            {
                XvcRegion.XVC_REGION_ID_HEADER,
                XvcRegion.XVC_REGION_ID_EMBEDDED_XVD,
                XvcRegion.XVC_REGION_ID_MUTABLE_XVC_METADATA,
                XvcRegion.XVC_REGION_ID_XVC_METADATA
            };

            foreach (var id in nonHashedIds)
            {
                var region = new XvcRegion { Id = id };
                // Act & Assert
                Assert.IsFalse(region.IsHashed, $"Expected IsHashed to be false for ID {id:X}");
            }
        }

        [TestMethod]
        public void IsHashed_WithHashedId_ReturnsTrue()
        {
            // Arrange
            // Making sure that the region is recognized as a region that is hashed with a non hashed id
            var region = new XvcRegion { Id = XvcRegion.XVC_REGION_ID_XVC_FILE_SYSTEM };

            // Act & Assert
            Assert.IsTrue(region.IsHashed);
        }

        [TestMethod]
        public void IsEncrypted_WithNoneKeyIndex_ReturnsFalse()
        {
            // Arrange
            // Making sure that the region is not encrypted when KeyIndex is set to XVC_KEY_INDEX_NONE
            var region = new XvcRegion { KeyIndex = XvcRegion.XVC_KEY_INDEX_NONE };

            // Act & Assert
            Assert.IsFalse(region.IsEncrypted);
        }

        [TestMethod]
        public void IsEncrypted_WithValidKeyIndex_ReturnsTrue()
        {
            // Arrange
            // Making sure that the region is encrypted when KeyIndex is set to a valid value
            var region = new XvcRegion { KeyIndex = 0x1234 };

            // Act & Assert
            Assert.IsTrue(region.IsEncrypted);
        }

        [TestMethod]
        public void EndPageOffset_CalculatesCorrectly()
        {
            // Arrange
            var region = new XvcRegion
            {
                Offset = 0x1000,
                Length = 0x5000
            };

            // Act
            // Making sure that the EndPageOffset is calculated correctly
            var result = region.EndPageOffset;

            // Assert - should be (0x1000 + 0x5000) / 4096 rounded up
            uint expected = (uint)Math.Ceiling((0x1000 + 0x5000) / (double)XvcRegion.XVD_PAGE_SIZE);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void PrintableId_WithSystemId_FormatsAsHex()
        {
            // Arrange
            var region = new XvcRegion { Id = 0x40000001 };

            // Act
            // Making sure that the PrintableId is formatted correctly
            var result = region.PrintableId;

            // Assert
            Assert.AreEqual("40000001", result);
        }

        [TestMethod]
        public void PrintableId_WithNonSystemId_FormatsAsDecimal()
        {
            // Arrange
            var region = new XvcRegion { Id = 123 };

            // Act
            // Making sure that the PrintableId is formatted correctly
            var result = region.PrintableId;

            // Assert
            Assert.AreEqual("123", result);
        }

        private byte[] CreateXvcRegionTestData(
            // Helper method to create test data for XvcRegion
            uint id, ushort keyIndex, ushort spare0, XvcRegionFlags flags,
            uint firstSegmentIndex, string description, ulong offset, ulong length, byte[] hash)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.Unicode))
            {
                writer.Write(id);
                writer.Write(keyIndex);
                writer.Write(spare0);
                writer.Write((uint)flags);
                writer.Write(firstSegmentIndex);

                // Write description (padded to full length)
                byte[] descBytes = new byte[XvcRegion.XVC_REGION_MAX_DESCRIPTION_CHARS * 2];
                byte[] actualDescBytes = Encoding.Unicode.GetBytes(description);
                Array.Copy(actualDescBytes, descBytes, Math.Min(actualDescBytes.Length, descBytes.Length));
                writer.Write(descBytes);

                writer.Write(offset);
                writer.Write(length);
                writer.Write(hash);

                // Write reserved space
                writer.Write(new byte[sizeof(ulong) * 3]);

                return ms.ToArray();
            }
        }

        private XvcRegion CreateXvcRegion(byte[] hash)
        {
            return new XvcRegion
            {
                Id = 0x12345678,
                Hash = hash
            };
        }
    }
}