// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

[Flags]
public enum XvcRegionFlags
{
    XVC_REGION_FLAG_SYSTEM_METADATA = 0x00000001,
    XVC_REGION_FLAG_PREVIEW = 0x00000002,
    XVC_REGION_FLAG_INITIAL_PLAY = 0x00000004,
    XVC_REGION_FLAG_ON_DEMAND = 0x00000008,
    XVC_REGION_FLAG_UNENCRYPTED = 0x00000010
}
public class XvcRegion
{
    public const int XVC_REGION_MAX_DESCRIPTION_CHARS = 32;
    public const int REGION_HASH_SIZE = 8;
    public const int XVD_PAGE_SIZE = 4096;
    public const UInt16 XVC_KEY_INDEX_NONE = 0xffff;

    public const UInt32 XVC_REGION_ID_INVALID = 0x00000000;
    public const UInt32 XVC_REGION_ID_XVC_METADATA = 0x40000001;
    public const UInt32 XVC_REGION_ID_XVC_FILE_SYSTEM = 0x40000002;
    public const UInt32 XVC_REGION_ID_REGISTRATION_FILES = 0x40000003;
    public const UInt32 XVC_REGION_ID_EMBEDDED_XVD = 0x40000004;
    public const UInt32 XVC_REGION_ID_HEADER = 0x40000005;
    public const UInt32 XVC_REGION_ID_MUTABLE_XVC_METADATA = 0x40000006;

    public UInt32 Id;
    public UInt16 KeyIndex;
    public UInt16 Spare0;
    public XvcRegionFlags Flags;
    public UInt32 FirstSegmentIndex;
    public string Description = string.Empty; // XVC_REGION_MAX_DESCRIPTION_CHARS
    public UInt64 Offset;
    public UInt64 Length;
    public byte[] Hash = []; // REGION_HASH_SIZE
                        // UInt64[] Reserved; // 3
    public bool IsSystem => Id >= 0x3FFFFFFF;
    public bool IsOnDemand => Flags.HasFlag(XvcRegionFlags.XVC_REGION_FLAG_ON_DEMAND);
    public bool IsHashed => Id != XVC_REGION_ID_HEADER && Id != XVC_REGION_ID_EMBEDDED_XVD && Id != XVC_REGION_ID_MUTABLE_XVC_METADATA && Id != XVC_REGION_ID_XVC_METADATA;
    public bool IsEncrypted => KeyIndex != XVC_KEY_INDEX_NONE;
    public UInt32 EndPageOffset => XvcFile.BytesToPages(Offset + Length);

    public string PrintableId => Id.ToString(IsSystem ? "X" : "D");

    public static XvcRegion Read(Stream stream)
    {
        using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
        {
            var header = new XvcRegion();
            header.Id = reader.ReadUInt32();
            header.KeyIndex = reader.ReadUInt16();
            header.Spare0 = reader.ReadUInt16();
            header.Flags = (XvcRegionFlags)reader.ReadUInt32();
            header.FirstSegmentIndex = reader.ReadUInt32();
            header.Description = Encoding.Unicode.GetString(reader.ReadBytes(XVC_REGION_MAX_DESCRIPTION_CHARS * 2));
            header.Offset = reader.ReadUInt64();
            header.Length = reader.ReadUInt64();
            header.Hash = reader.ReadBytes(REGION_HASH_SIZE);
            reader.ReadBytes(sizeof(UInt64) * 3);
            return header;
        }
    }

    public bool IsHashSame(XvcRegion other)
    {
        for (int i = 0; i < Hash.Length; ++i)
        {
            if (Hash[i] != other.Hash[i])
                return false;
        }
        return true;
    }
}