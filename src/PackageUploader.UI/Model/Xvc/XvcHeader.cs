// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

[Flags]
public enum XvcHeaderFlags
{
    XVC_HEADER_FLAG_XTS_OFFSETS_CACHED = 0x00000001,
    XVC_HEADER_FLAG_STRICT_REGION_MATCH = 0x00000002,
    XVC_HEADER_FLAG_SUB_FILE_UPDATE_REQUESTED = 0x00000004
}

public class XvcHeader
{
    public const int Size = 0xda8;

    public const int XVC_MAX_KEY_COUNT = 64;
    public const int XVC_MAX_DESCRIPTION_CHARS = 128;

    public Guid Id;
    public Guid[] KeyId; // XVC_MAX_KEY_COUNT
    //ULONG64 Reserved0[16][16];
    public string Description; // XVC_MAX_DESCRIPTION_CHARS
    public uint Version;
    public uint NumberRegions;
    public XvcHeaderFlags Flags;
    public ushort LangId;
    public ushort NumberKeyIds;
    public uint Type;
    public uint InitialPlayRegionId;
    public ulong InitialPlayOffset;
    public ulong CreationTime;
    public uint PreviewRegionId;
    public uint NumberSegments;
    public ulong PreviewOffset;
    public ulong UnusedLength;
    public uint NumberRegionSpecifiers;
    public uint NumberXtsEntries;
    //ULONG64 Reserved2[10];

    private XvcHeader(BinaryReader reader)
    {
        Id = reader.ReadGuid();
        KeyId = reader.ReadGuids(XVC_MAX_KEY_COUNT);
        reader.ReadBytes(sizeof(ulong) * 16 * 16);
        Description = reader.ReadNullTerminatedString(XVC_MAX_DESCRIPTION_CHARS);
        Version = reader.ReadUInt32();
        NumberRegions = reader.ReadUInt32();
        Flags = (XvcHeaderFlags)reader.ReadUInt32();
        LangId = reader.ReadUInt16();
        NumberKeyIds = reader.ReadUInt16();
        Type = reader.ReadUInt32();
        InitialPlayRegionId = reader.ReadUInt32();
        InitialPlayOffset = reader.ReadUInt64();
        CreationTime = reader.ReadUInt64();
        PreviewRegionId = reader.ReadUInt32();
        NumberSegments = reader.ReadUInt32();
        PreviewOffset = reader.ReadUInt64();
        UnusedLength = reader.ReadUInt64();
        NumberRegionSpecifiers = reader.ReadUInt32();
        NumberXtsEntries = reader.ReadUInt32();
        reader.ReadBytes(sizeof(ulong) * 10);
    }

    public static XvcHeader Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        return new XvcHeader(reader);
    }
}
