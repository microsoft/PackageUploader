// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System.Text;

namespace PackageUploader.UI.Model;

[Flags]
public enum XvdFeatureBits
{
    ReadOnly = 1,
    EncryptionDisabled = 2,
    DataIntegrityDisabled = 4,
    LegacySectorSize = 8,
    ResiliencyEnabled = 16,
    SraReadOnly = 32,
    RegionIdInXts = 64,
    EraSpecific = 128
}

[Flags]
public enum XvdPlatform
{
    Xbox = 0,
    PC = 1,
    Gen8GameCore = 2,
    ScarlettGameCore = 4
}

public enum XvdContentType
{
    Data = 0,
    Title = 1,
    SystemOS = 2,
    EraOS = 3,
    Scratch = 4,
    ResetData = 5,
    App = 6,
    HostOS = 7,
    Xbox360STFS = 8,
    Xbox360FATX = 9,
    Xbox360GDFX = 10,
    Updater = 11,
    OfflineUpdater = 12,
    Template = 13,
    MteHost = 14,
    MteApp = 15,
    MteTitle = 16,
    MteEraOS = 17,
    EraTools = 18,
    SystemTools = 19,
    SystemAuxiliary = 20,
    AcousticModel = 21,
    Codec = 22,
    Qaslt = 23,
    AppDlc = 24,
    TitleDlc = 25,
    UniversalDlc = 26,
    SystemData = 27,
    Test = 28,
    HwTest = 29,
    KioskData = 30,
    DevPackage = 31,
    HostProfiler = 32,
    Roamable = 33,
    ThinProvisioned = 34,
    StreamingOnlySra = 35,
    StreamingOnlyEra = 36,
    StreamingOnlyHost = 37,
    QuickResume = 38,
    HostData = 39
}

public class XvdHeader
{
    public const int Size = 4096;

    public const int XVD_SIGNATURE_LENGTH = 0x200;
    public const int XVD_ROOT_HASH_LENGTH = 32;
    public const int XVD_KEY_LENGTH_BYTES = (256 / 8);
    public const int XVD_SANDBOX_ID_LENGTH = 16;
    public const int XVD_MAX_PE_CATALOGS = 4;
    public const int XVD_MAX_PE_CATALOG_CAPS = 4;

    //UCHAR Signature[XVD_SIGNATURE_LENGTH];
    public UInt64 Cookie;
    public XvdFeatureBits FeatureBits;
    public UInt32 FormatVersion;
    public UInt64 CreationTime;
    public UInt64 DriveSize;
    public Guid VDUID;
    public Guid UVUID;
    public Byte[] RootHash; // XVD_ROOT_HASH_LENGTH
    public Byte[] XvcHash; // XVD_ROOT_HASH_LENGTH
    public UInt32 Type;
    public XvdContentType ContentType;
    public UInt32 EmbeddedXVDLength;
    public UInt32 UserDataLength;
    public UInt32 XVCLength;
    public UInt32 DynHeaderLength;
    public UInt32 BlockSize;
    //XVD_EXT_ENTRY ExtEntry; // 4
    public UInt16[] Capabilities; // 8
    public Byte[] PECatalogHash; // XVD_ROOT_HASH_LENGTH
    public Guid EmbeddedXvdPDUID;
    //UCHAR Reserved0[16];
    public Byte[] KeyMaterial; // XVD_KEY_LENGTH_BYTES
    public Byte[] UserDataHash; // XVD_ROOT_HASH_LENGTH
    public Byte[] SandboxId; // XVD_SANDBOX_ID_LENGTH
    public Guid ProductId;
    public Guid PDUID;
    public UInt64 PackageVersionNumber;
    //USHORT PECatalogCaps[XVD_MAX_PE_CATALOGS][XVD_MAX_PE_CATALOG_CAPS];
    //UCHAR PECatalogs[XVD_MAX_PE_CATALOGS][XVD_ROOT_HASH_LENGTH];
    public UInt32 WriteableExpirationDate;
    public UInt32 WriteablePolicyFlags;
    public UInt32 PlsSize;
    public Byte MutableXVCPageCount;
    public XvdPlatform Platform;
    //UCHAR UnusedBytes[27];
    public UInt64 SequenceNumber;
    public UInt64 MinSysVer;
    public UInt32 OdkId;
    //UCHAR RoamingHeader[2024];
    //UCHAR Reserved[XVD_HEADER_READ_ONLY_RESERVED];

    public string SandboxIdString => UTF8Encoding.UTF8.GetString(SandboxId);
    public string PackageVersionNumberString => $"{(PackageVersionNumber >> 48) & 0xffff}.{(PackageVersionNumber >> 32) & 0xffff}.{(PackageVersionNumber >> 16) & 0xffff}.{PackageVersionNumber & 0xffff}";
    public string MinSysVerString => $"{(MinSysVer >> 48) & 0x7fff}.{(MinSysVer >> 32) & 0xffff}.{(MinSysVer >> 16) & 0xffff}.{MinSysVer & 0xffff}";

    private XvdHeader(BinaryReader reader)
    {
        reader.ReadBytes(XVD_SIGNATURE_LENGTH);
        Cookie = reader.ReadUInt64();
        FeatureBits = (XvdFeatureBits)reader.ReadUInt32();
        FormatVersion = reader.ReadUInt32();
        CreationTime = reader.ReadUInt64();
        DriveSize = reader.ReadUInt64();
        VDUID = reader.ReadGuid();
        UVUID = reader.ReadGuid();
        RootHash = reader.ReadBytes(XVD_ROOT_HASH_LENGTH);
        XvcHash = reader.ReadBytes(XVD_ROOT_HASH_LENGTH);
        Type = reader.ReadUInt32();
        ContentType = (XvdContentType)reader.ReadUInt32();
        EmbeddedXVDLength = reader.ReadUInt32();
        UserDataLength = reader.ReadUInt32();
        XVCLength = reader.ReadUInt32();
        DynHeaderLength = reader.ReadUInt32();
        BlockSize = reader.ReadUInt32();
        reader.ReadBytes(4 * (sizeof(UInt32) * 4 + sizeof(UInt64))); // ExtEntry
        Capabilities = [.. Enumerable.Range(0, 8).Select(i => reader.ReadUInt16())];
        PECatalogHash = reader.ReadBytes(XVD_ROOT_HASH_LENGTH);
        EmbeddedXvdPDUID = reader.ReadGuid();
        reader.ReadBytes(16); // Reserved0
        KeyMaterial = reader.ReadBytes(XVD_KEY_LENGTH_BYTES);
        UserDataHash = reader.ReadBytes(XVD_ROOT_HASH_LENGTH);
        SandboxId = reader.ReadBytes(XVD_SANDBOX_ID_LENGTH);
        ProductId = reader.ReadGuid();
        PDUID = reader.ReadGuid();
        PackageVersionNumber = reader.ReadUInt64();
        reader.ReadBytes(XVD_MAX_PE_CATALOGS * XVD_MAX_PE_CATALOG_CAPS * sizeof(UInt16));
        reader.ReadBytes(XVD_MAX_PE_CATALOGS * XVD_ROOT_HASH_LENGTH);
        WriteableExpirationDate = reader.ReadUInt32();
        WriteablePolicyFlags = reader.ReadUInt32();
        PlsSize = reader.ReadUInt32();
        MutableXVCPageCount = reader.ReadByte();
        Platform = (XvdPlatform)reader.ReadByte();
        reader.ReadBytes(26); // UnusedBytes
        SequenceNumber = reader.ReadUInt64();
        MinSysVer = reader.ReadUInt64();
        OdkId = reader.ReadUInt32();
    }

    public static XvdHeader Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        return new XvdHeader(reader);
    }
}
