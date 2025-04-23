// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public class SegmentMetadata
{
    public uint Magic { get; private set; }
    public uint VersionMajor { get; private set; }
    public uint VersionMinor { get; private set; }
    public uint HeaderSize { get; private set; }
    public int SegmentCount { get; private set; }
    public uint PathDataSize { get; private set; }
    public Guid PDUID { get; private set; }
    public uint Flags { get; private set; }

    private const int SIZE_OF_HEADER = 100;
    private const int SIZE_OF_SEGMENT = 16;

    private Segment[] Segments = [];

    private struct Segment
    {
        public ushort Flags;
        public ushort SourcePathLength;
        public int SourcePathOffset;
        public UInt64 ByteAccurateFileSize;
        public string SourcePath;
    }

    public static SegmentMetadata Read(byte[] bytes)
    {
        var segmentMetadata = new SegmentMetadata();

        using (var r = new BinaryReader(new MemoryStream(bytes)))
        {
            segmentMetadata.Magic = r.ReadUInt32();
            segmentMetadata.VersionMajor = r.ReadUInt32();
            segmentMetadata.VersionMinor = r.ReadUInt32();
            segmentMetadata.HeaderSize = r.ReadUInt32();
            segmentMetadata.SegmentCount = r.ReadInt32();
            segmentMetadata.PathDataSize = r.ReadUInt32();
            segmentMetadata.PDUID = r.ReadGuid();
            segmentMetadata.Flags = r.ReadUInt32();
            r.ReadBytes(56);

            segmentMetadata.Segments = new Segment[segmentMetadata.SegmentCount];
            var pathBlock = SIZE_OF_HEADER + segmentMetadata.SegmentCount * SIZE_OF_SEGMENT;
            for (int i = 0; i < segmentMetadata.SegmentCount; i++)
            {
                var flags = r.ReadUInt16();
                var sourcePathLength = r.ReadUInt16();
                var sourcePathOffset = r.ReadInt32();
                var byteAccurateFileSize = r.ReadUInt64();
                var sourcePath = Encoding.Unicode.GetString(bytes, pathBlock + sourcePathOffset, sourcePathLength * 2);
                segmentMetadata.Segments[i] = new Segment()
                {
                    Flags = flags,
                    SourcePathLength = sourcePathLength,
                    SourcePathOffset = sourcePathOffset,
                    ByteAccurateFileSize = byteAccurateFileSize,
                    SourcePath = sourcePath
                };
            }
        }

        return segmentMetadata;
    }

    public string GetPath(int segmentIndex)
    {
        return Segments[segmentIndex].SourcePath;
    }

    public UInt64 GetSize(int segmentIndex)
    {
        return Segments[segmentIndex].ByteAccurateFileSize;
    }
}

