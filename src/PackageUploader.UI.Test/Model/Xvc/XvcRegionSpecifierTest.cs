using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using PackageUploader.UI.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace PackageUploader.UI.Test.Model
{
    [TestClass]
    public class XvcRegionSpecifierTest
    {
        [TestMethod]
        public void Read_ValidStream_ReturnsCorrectObject()
        {
            // Arrange
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true))
            {
                writer.Write((uint)123); // RegionId
                writer.Write((uint)456); // Flags

                // Write null-terminated Key
                byte[] keyBytes = WriteNullTerminatedString("TestKey", XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH);
                writer.Write(keyBytes);

                // Write null-terminated Value
                byte[] valueBytes = WriteNullTerminatedString("TestValue", XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH);
                writer.Write(valueBytes);
            }

            stream.Position = 0;

            // Act
            var specifier = XvcRegionSpecifier.Read(stream);

            // Assert
            Assert.AreEqual((uint)123, specifier.RegionId);
            Assert.AreEqual((uint)456, specifier.Flags);
            Assert.AreEqual("TestKey", specifier.Key);
            Assert.AreEqual("TestValue", specifier.Value);
        }

        [TestMethod]
        public void Read_EmptyStrings_ReturnsEmptyStrings()
        {
            // Arrange
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true))
            {
                writer.Write((uint)0); // RegionId
                writer.Write((uint)0); // Flags

                // Write empty Key with just null terminator
                byte[] keyBytes = WriteNullTerminatedString("", XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH);
                writer.Write(keyBytes);

                // Write empty Value with just null terminator
                byte[] valueBytes = WriteNullTerminatedString("", XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH);
                writer.Write(valueBytes);
            }

            stream.Position = 0;

            // Act
            var specifier = XvcRegionSpecifier.Read(stream);

            // Assert
            Assert.AreEqual(string.Empty, specifier.Key);
            Assert.AreEqual(string.Empty, specifier.Value);
        }

        [TestMethod]
        public void Read_MaxLengthStrings_ReturnsCorrectStrings()
        {
            // Arrange
            string maxKey = new string('K', XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH - 1); // Leave room for null terminator
            string maxValue = new string('V', XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH - 1); // Leave room for null terminator

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true))
            {
                writer.Write((uint)0); // RegionId
                writer.Write((uint)0); // Flags

                // Write max length Key
                byte[] keyBytes = WriteNullTerminatedString(maxKey, XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH);
                writer.Write(keyBytes);

                // Write max length Value
                byte[] valueBytes = WriteNullTerminatedString(maxValue, XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH);
                writer.Write(valueBytes);
            }

            stream.Position = 0;

            // Act
            var specifier = XvcRegionSpecifier.Read(stream);

            // Assert
            Assert.AreEqual(maxKey, specifier.Key);
            Assert.AreEqual(maxValue, specifier.Value);
        }

        [TestMethod]
        public void Constants_HaveCorrectValues()
        {
            // Assert
            Assert.AreEqual(64, XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH);
            Assert.AreEqual(128, XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH);
            Assert.AreEqual("Languages", XvcRegionSpecifier.Languages);
            Assert.AreEqual("Devices", XvcRegionSpecifier.Devices);
            Assert.AreEqual("Tags", XvcRegionSpecifier.Tags);
            Assert.AreEqual("ContentTypes", XvcRegionSpecifier.ContentTypes);
        }

        [TestMethod]
        public void Read_PredefinedKeyValues_ReturnsCorrectStrings()
        {
            // Test each of the predefined keys
            string[] predefinedKeys = {
                XvcRegionSpecifier.Languages,
                XvcRegionSpecifier.Devices,
                XvcRegionSpecifier.Tags,
                XvcRegionSpecifier.ContentTypes
            };

            foreach (var key in predefinedKeys)
            {
                // Arrange
                using var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true))
                {
                    writer.Write((uint)1); // RegionId
                    writer.Write((uint)0); // Flags

                    // Write key
                    byte[] keyBytes = WriteNullTerminatedString(key, XvcRegionSpecifier.XVC_REGION_SPECIFIER_KEY_LENGTH);
                    writer.Write(keyBytes);

                    // Write a value
                    byte[] valueBytes = WriteNullTerminatedString("TestValue", XvcRegionSpecifier.XVC_REGION_SPECIFIER_VALUE_LENGTH);
                    writer.Write(valueBytes);
                }

                stream.Position = 0;

                // Act
                var specifier = XvcRegionSpecifier.Read(stream);

                // Assert
                Assert.AreEqual(key, specifier.Key);
                Assert.AreEqual("TestValue", specifier.Value);
            }
        }

        // Helper method to simulate writing a null-terminated string with fixed length buffer
        private byte[] WriteNullTerminatedString(string value, int maxLength)
        {
            byte[] buffer = new byte[maxLength * 2]; // Unicode = 2 bytes per char

            if (!string.IsNullOrEmpty(value))
            {
                byte[] stringBytes = Encoding.Unicode.GetBytes(value);
                int bytesToCopy = Math.Min(stringBytes.Length, buffer.Length - 2); // Leave room for null terminator
                Array.Copy(stringBytes, buffer, bytesToCopy);
            }

            // Ensure null termination (two zero bytes for unicode)
            // The null terminator should be placed after the string content
            int nullTerminatorPosition = Math.Min(value?.Length ?? 0, maxLength - 1) * 2;
            buffer[nullTerminatorPosition] = 0;
            buffer[nullTerminatorPosition + 1] = 0;

            return buffer;
        }
    }
}