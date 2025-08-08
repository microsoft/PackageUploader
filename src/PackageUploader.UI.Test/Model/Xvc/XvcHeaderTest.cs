namespace PackageUploader.UI.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

[TestClass]
public class XvcHeaderTest
{
    private MemoryStream _testStream;
    private XvcHeader _xvcHeader;
    private Guid _testId;
    private Guid _testKeyId;
    private string _testDescription;
    private uint _testVersion;
    private uint _testNumberRegions;
    private XvcHeaderFlags _testFlags;
    private ushort _testLangId;
    private ushort _testNumberKeyIds;
    private uint _testType;
    private uint _testInitialPlayRegionId;
    private ulong _testInitialPlayOffset;
    private ulong _testCreationTime;
    private uint _testPreviewRegionId;
    private uint _testNumberSegments;
    private ulong _testPreviewOffset;
    private ulong _testUnusedLength;
    private uint _testNumberRegionSpecifiers;
    private uint _testNumberXtsEntries;

    [TestInitialize]
    public void Setup()
    {
        _testStream = new MemoryStream();
        _testId = Guid.NewGuid();
        _testKeyId = Guid.NewGuid();
        _testDescription = "Test XVC Header";
        _testVersion = 1;
        _testNumberRegions = 5;
        _testFlags = XvcHeaderFlags.XVC_HEADER_FLAG_XTS_OFFSETS_CACHED | XvcHeaderFlags.XVC_HEADER_FLAG_STRICT_REGION_MATCH;
        _testLangId = 0x0409; // English (United States)
        _testNumberKeyIds = 1;
        _testType = 1;
        _testInitialPlayRegionId = 2;
        _testInitialPlayOffset = 1000;
        _testCreationTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _testPreviewRegionId = 3;
        _testNumberSegments = 4;
        _testPreviewOffset = 2000;
        _testUnusedLength = 3000;
        _testNumberRegionSpecifiers = 2;
        _testNumberXtsEntries = 3;

        using (BinaryWriter writer = new BinaryWriter(_testStream, Encoding.UTF8, true))
        {
            // Write test data
            writer.Write(_testId.ToByteArray());

            // Write KeyIds
            Guid[] keyIds = new Guid[XvcHeader.XVC_MAX_KEY_COUNT];
            keyIds[0] = _testKeyId;
            foreach (var key in keyIds)
            {
                writer.Write(key.ToByteArray());
            }

            // Reserved0 field
            writer.Write(new byte[sizeof(ulong) * 16 * 16]);

            // Write description with padding
            byte[] descriptionBytes = Encoding.Unicode.GetBytes(_testDescription.PadRight(XvcHeader.XVC_MAX_DESCRIPTION_CHARS, '\0'));
            writer.Write(descriptionBytes);

            writer.Write(_testVersion);
            writer.Write(_testNumberRegions);
            writer.Write((uint)_testFlags);
            writer.Write(_testLangId);
            writer.Write(_testNumberKeyIds);
            writer.Write(_testType);
            writer.Write(_testInitialPlayRegionId);
            writer.Write(_testInitialPlayOffset);
            writer.Write(_testCreationTime);
            writer.Write(_testPreviewRegionId);
            writer.Write(_testNumberSegments);
            writer.Write(_testPreviewOffset);
            writer.Write(_testUnusedLength);
            writer.Write(_testNumberRegionSpecifiers);
            writer.Write(_testNumberXtsEntries);

            // Reserved2 field
            writer.Write(new byte[sizeof(ulong) * 10]);
        }

        _testStream.Position = 0;
        _xvcHeader = XvcHeader.Read(_testStream);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _testStream.Dispose();
    }

    [TestMethod]
    public void XvcHeader_ReadTest_BasicProperties()
    {
        // Assert
        Assert.AreEqual(_testId, _xvcHeader.Id, "Id property doesn't match expected value");
        Assert.AreEqual(_testDescription.TrimEnd('\0'), _xvcHeader.Description.TrimEnd('\0'), "Description property doesn't match expected value");
        Assert.AreEqual(_testVersion, _xvcHeader.Version, "Version property doesn't match expected value");
        Assert.AreEqual(_testNumberRegions, _xvcHeader.NumberRegions, "NumberRegions property doesn't match expected value");
        Assert.AreEqual(_testFlags, _xvcHeader.Flags, "Flags property doesn't match expected value");
        Assert.AreEqual(_testLangId, _xvcHeader.LangId, "LangId property doesn't match expected value");
        Assert.AreEqual(_testNumberKeyIds, _xvcHeader.NumberKeyIds, "NumberKeyIds property doesn't match expected value");
    }

    [TestMethod]
    public void XvcHeader_ReadTest_AdvancedProperties()
    {
        // Assert
        Assert.AreEqual(_testType, _xvcHeader.Type, "Type property doesn't match expected value");
        Assert.AreEqual(_testInitialPlayRegionId, _xvcHeader.InitialPlayRegionId, "InitialPlayRegionId property doesn't match expected value");
        Assert.AreEqual(_testInitialPlayOffset, _xvcHeader.InitialPlayOffset, "InitialPlayOffset property doesn't match expected value");
        Assert.AreEqual(_testCreationTime, _xvcHeader.CreationTime, "CreationTime property doesn't match expected value");
        Assert.AreEqual(_testPreviewRegionId, _xvcHeader.PreviewRegionId, "PreviewRegionId property doesn't match expected value");
        Assert.AreEqual(_testNumberSegments, _xvcHeader.NumberSegments, "NumberSegments property doesn't match expected value");
        Assert.AreEqual(_testPreviewOffset, _xvcHeader.PreviewOffset, "PreviewOffset property doesn't match expected value");
        Assert.AreEqual(_testUnusedLength, _xvcHeader.UnusedLength, "UnusedLength property doesn't match expected value");
        Assert.AreEqual(_testNumberRegionSpecifiers, _xvcHeader.NumberRegionSpecifiers, "NumberRegionSpecifiers property doesn't match expected value");
        Assert.AreEqual(_testNumberXtsEntries, _xvcHeader.NumberXtsEntries, "NumberXtsEntries property doesn't match expected value");
    }

    [TestMethod]
    public void XvcHeader_ReadTest_KeyIds()
    {
        // Assert
        Assert.IsNotNull(_xvcHeader.KeyId, "KeyId array should not be null");
        Assert.AreEqual(XvcHeader.XVC_MAX_KEY_COUNT, _xvcHeader.KeyId.Length, "KeyId array should have the expected length");
        Assert.AreEqual(_testKeyId, _xvcHeader.KeyId[0], "First KeyId doesn't match expected value");
    }
}
