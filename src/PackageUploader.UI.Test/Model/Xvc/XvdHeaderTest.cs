using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PackageUploader.UI.Test.Model
{
    [TestClass]
    public class XvdHeaderTest
    {
        [TestMethod]
        public void Read_ValidStream_ReturnsCorrectObject()
        {
            // Arrange
            // Making sure that the header has the correct data for the fields
            byte[] headerData = CreateXvdHeaderData();
            using var stream = new MemoryStream(headerData);

            // Act
            var header = XvdHeader.Read(stream);

            // Assert
            Assert.AreEqual((ulong)0x123456789ABCDEF0, header.Cookie);
            Assert.AreEqual(XvdFeatureBits.ReadOnly | XvdFeatureBits.EncryptionDisabled, header.FeatureBits);
            Assert.AreEqual((uint)42, header.FormatVersion);
            Assert.AreEqual((ulong)0x9876543210ABCDEF, header.DriveSize);
            Assert.AreEqual(new Guid("12345678-1234-5678-1234-567812345678"), header.VDUID);
            Assert.AreEqual(XvdContentType.Title, header.ContentType);
            Assert.AreEqual((uint)1024, header.EmbeddedXVDLength);
            Assert.AreEqual(XvdPlatform.Xbox | XvdPlatform.PC, header.Platform);
        }

        [TestMethod]
        public void SandboxIdString_ReturnsCorrectString()
        {
            // Arrange
            // Making sure that the SandboxIdString is read correctly
            byte[] headerData = CreateXvdHeaderData();
            using var stream = new MemoryStream(headerData);
            var header = XvdHeader.Read(stream);

            // Act
            string sandboxIdString = header.SandboxIdString;

            // Assert
            Assert.AreEqual("TestSandboxID\0\0\0", sandboxIdString);
        }

        [TestMethod]
        public void PackageVersionNumberString_ReturnsCorrectFormat()
        {
            // Arrange
            // Making sure that the header has the correct format for PackageVersionNumberString
            byte[] headerData = CreateXvdHeaderData();
            using var stream = new MemoryStream(headerData);
            var header = XvdHeader.Read(stream);
            // In the test data we set PackageVersionNumber to 0x0001000200030004

            // Act
            string versionString = header.PackageVersionNumberString;

            // Assert
            Assert.AreEqual("1.2.3.4", versionString);
        }

        [TestMethod]
        public void MinSysVerString_ReturnsCorrectFormat()
        {
            // Arrange
            // Making sure that the header has the correct format for MinSysVerString
            byte[] headerData = CreateXvdHeaderData();
            using var stream = new MemoryStream(headerData);
            var header = XvdHeader.Read(stream);
            // In the test data we set MinSysVer to 0x8000000500060007

            // Act
            string minSysVerString = header.MinSysVerString;

            // Assert
            Assert.AreEqual("0.5.6.7", minSysVerString);
        }

        [TestMethod]
        public void Read_ValidatesAllFields()
        {
            // Arrange
            byte[] headerData = CreateXvdHeaderData();
            using var stream = new MemoryStream(headerData);

            // Act
            var header = XvdHeader.Read(stream);

            // Assert
            //Checking if each field is correctly read based on the test data
            Assert.IsNotNull(header.RootHash);
            Assert.AreEqual(XvdHeader.XVD_ROOT_HASH_LENGTH, header.RootHash.Length);
            Assert.AreEqual(1, header.RootHash[0]);
            Assert.AreEqual(32, header.RootHash[31]);

            Assert.IsNotNull(header.XvcHash);
            Assert.AreEqual(XvdHeader.XVD_ROOT_HASH_LENGTH, header.XvcHash.Length);

            Assert.IsNotNull(header.Capabilities);
            Assert.AreEqual(8, header.Capabilities.Length);
            Assert.AreEqual((ushort)0x1234, header.Capabilities[0]);
            Assert.AreEqual((ushort)0x5678, header.Capabilities[1]);

            Assert.IsNotNull(header.KeyMaterial);
            Assert.AreEqual(XvdHeader.XVD_KEY_LENGTH_BYTES, header.KeyMaterial.Length);

            Assert.IsNotNull(header.UserDataHash);
            Assert.AreEqual(XvdHeader.XVD_ROOT_HASH_LENGTH, header.UserDataHash.Length);

            Assert.AreEqual(new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFFFFFF"), header.ProductId);
            Assert.AreEqual(new Guid("BBBBBBBB-CCCC-DDDD-EEEE-FFFFFFFFFFFF"), header.PDUID);

            Assert.AreEqual((uint)0x12345678, header.WriteableExpirationDate);
            Assert.AreEqual((uint)0x87654321, header.WriteablePolicyFlags);
            Assert.AreEqual((uint)0x55555555, header.PlsSize);
            Assert.AreEqual((byte)0x42, header.MutableXVCPageCount);
            Assert.AreEqual((ulong)0x9999AAAA8888BBBB, header.SequenceNumber);
            Assert.AreEqual((uint)0x77777777, header.OdkId);
        }

        private byte[] CreateXvdHeaderData()
        {
            // This helper method is used to create a XvdHeader with arbitrary data for testing
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Signature
            writer.Write(new byte[XvdHeader.XVD_SIGNATURE_LENGTH]);

            // Basic header fields
            writer.Write((ulong)0x123456789ABCDEF0);  // Cookie
            writer.Write((uint)(XvdFeatureBits.ReadOnly | XvdFeatureBits.EncryptionDisabled));  // FeatureBits
            writer.Write((uint)42);  // FormatVersion
            writer.Write((ulong)0x1234567890ABCDEF);  // CreationTime
            writer.Write((ulong)0x9876543210ABCDEF);  // DriveSize
            writer.Write(new Guid("12345678-1234-5678-1234-567812345678").ToByteArray());  // VDUID
            writer.Write(new Guid("87654321-4321-8765-4321-876543214321").ToByteArray());  // UVUID

            // RootHash
            byte[] rootHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
            for (int i = 0; i < rootHash.Length; i++)
                rootHash[i] = (byte)(i + 1);
            writer.Write(rootHash);

            // XvcHash
            byte[] xvcHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
            for (int i = 0; i < xvcHash.Length; i++)
                xvcHash[i] = (byte)(100 + i);
            writer.Write(xvcHash);

            writer.Write((uint)123);  // Type
            writer.Write((uint)XvdContentType.Title);  // ContentType
            writer.Write((uint)1024);  // EmbeddedXVDLength
            writer.Write((uint)2048);  // UserDataLength
            writer.Write((uint)4096);  // XVCLength
            writer.Write((uint)8192);  // DynHeaderLength
            writer.Write((uint)16384);  // BlockSize

            // ExtEntry (skipped in the class - 4 entries * 36 bytes)
            writer.Write(new byte[4 * (sizeof(uint) * 4 + sizeof(ulong))]);

            // Capabilities
            writer.Write((ushort)0x1234);
            writer.Write((ushort)0x5678);
            writer.Write(new byte[6 * sizeof(ushort)]);  // Remaining capabilities

            // PECatalogHash
            writer.Write(new byte[XvdHeader.XVD_ROOT_HASH_LENGTH]);

            writer.Write(new Guid("FFFFFFFF-EEEE-DDDD-CCCC-BBBBAAAAAAAA").ToByteArray());  // EmbeddedXvdPDUID
            writer.Write(new byte[16]);  // Reserved0

            // KeyMaterial
            writer.Write(new byte[XvdHeader.XVD_KEY_LENGTH_BYTES]);

            // UserDataHash
            writer.Write(new byte[XvdHeader.XVD_ROOT_HASH_LENGTH]);

            // SandboxId
            byte[] sandboxId = Encoding.UTF8.GetBytes("TestSandboxID");
            writer.Write(sandboxId);
            writer.Write(new byte[XvdHeader.XVD_SANDBOX_ID_LENGTH - sandboxId.Length]);  // Padding

            writer.Write(new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFFFFFF").ToByteArray());  // ProductId
            writer.Write(new Guid("BBBBBBBB-CCCC-DDDD-EEEE-FFFFFFFFFFFF").ToByteArray());  // PDUID

            writer.Write((ulong)0x0001000200030004);  // PackageVersionNumber (1.2.3.4)

            // PECatalogCaps and PECatalogs
            writer.Write(new byte[XvdHeader.XVD_MAX_PE_CATALOGS * XvdHeader.XVD_MAX_PE_CATALOG_CAPS * sizeof(ushort)]);
            writer.Write(new byte[XvdHeader.XVD_MAX_PE_CATALOGS * XvdHeader.XVD_ROOT_HASH_LENGTH]);

            writer.Write((uint)0x12345678);  // WriteableExpirationDate
            writer.Write((uint)0x87654321);  // WriteablePolicyFlags
            writer.Write((uint)0x55555555);  // PlsSize
            writer.Write((byte)0x42);  // MutableXVCPageCount
            writer.Write((byte)(XvdPlatform.Xbox | XvdPlatform.PC));  // Platform
            writer.Write(new byte[26]);  // UnusedBytes

            writer.Write((ulong)0x9999AAAA8888BBBB);  // SequenceNumber
            writer.Write((ulong)0x8000000500060007);  // MinSysVer (0.5.6.7 with high bit set)
            writer.Write((uint)0x77777777);  // OdkId

            return ms.ToArray();
        }
    }
}