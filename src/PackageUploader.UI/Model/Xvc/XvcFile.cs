// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

class XvcFile
{
    private const int XVD_PAGE_SIZE = 4096;
    private const int XVD_UNUSED_SPACE = 0x2000;
    private const int XVD_HASH_LENGTH = 24;
    private const int XVD_HASH_ENTRIES_PER_HASH_BLOCK = (XVD_PAGE_SIZE / XVD_HASH_LENGTH);

    private const int XVD_DATA_BLOCKS_PER_LEVEL0_HASH_TREE = XVD_HASH_ENTRIES_PER_HASH_BLOCK;
    private const int XVD_DATA_BLOCKS_PER_LEVEL1_HASH_TREE = XVD_HASH_ENTRIES_PER_HASH_BLOCK * XVD_DATA_BLOCKS_PER_LEVEL0_HASH_TREE;
    private const int XVD_DATA_BLOCKS_PER_LEVEL2_HASH_TREE = XVD_HASH_ENTRIES_PER_HASH_BLOCK * XVD_DATA_BLOCKS_PER_LEVEL1_HASH_TREE;
    private const int XVD_DATA_BLOCKS_PER_LEVEL3_HASH_TREE = XVD_HASH_ENTRIES_PER_HASH_BLOCK * XVD_DATA_BLOCKS_PER_LEVEL2_HASH_TREE;

    public static UInt32 BytesToPages(UInt64 bytes)
    {
        return (UInt32)(bytes / XVD_PAGE_SIZE);
    }

    private static UInt64 AlignBytesToPages(UInt64 bytes)
    {
        return (bytes + XVD_PAGE_SIZE - 1) & ~((UInt64)XVD_PAGE_SIZE - 1);
    }
    public static UInt64 PagesToBytes(UInt32 pages)
    {
        return (UInt64)pages * XVD_PAGE_SIZE;
    }

    private static UInt32 NumberOfHashPagesForLevel(UInt64 dataPages, Int32 level)
    {
        if (dataPages == 0)
        {
            return 0;
        }

        if (level > 0)
        {
            if (NumberOfHashPagesForLevel(dataPages, level - 1) <= 1)
            {
                return 0;
            }
        }

        var divisor = level switch
        {
            0 => (ulong)XVD_DATA_BLOCKS_PER_LEVEL0_HASH_TREE,
            1 => (ulong)XVD_DATA_BLOCKS_PER_LEVEL1_HASH_TREE,
            2 => (ulong)XVD_DATA_BLOCKS_PER_LEVEL2_HASH_TREE,
            3 => (ulong)XVD_DATA_BLOCKS_PER_LEVEL3_HASH_TREE,
            _ => throw new Exception(),
        };
        return (UInt32)((dataPages + divisor - 1) / divisor);
    }

    public static void GetBuildAndKeyId(string packagePath, out Guid buildId, out Guid keyId)
    {
        using FileStream stream = File.OpenRead(packagePath);

        XvdHeader xvdHeader = XvdHeader.Read(stream);

        UInt32 hashedPages = 0;

        if (!xvdHeader.FeatureBits.HasFlag(XvdFeatureBits.DataIntegrityDisabled))
        {
            hashedPages = BytesToPages(AlignBytesToPages(xvdHeader.DriveSize));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.UserDataLength));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.XVCLength));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.DynHeaderLength));
        }

        UInt64 unusedOffset = XvdHeader.Size;
        UInt64 embeddedOffset = unusedOffset + XVD_UNUSED_SPACE;
        UInt64 mutableXvcOffset = embeddedOffset + AlignBytesToPages(xvdHeader.EmbeddedXVDLength);
        UInt64 hashL3Offset = mutableXvcOffset + PagesToBytes(xvdHeader.MutableXVCPageCount);
        UInt64 hashL2Offset = hashL3Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 3));
        UInt64 hashL1Offset = hashL2Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 2));
        UInt64 hashL0Offset = hashL1Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 1));
        UInt64 userDataOffset = hashL0Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 0));
        UInt64 xvcHeaderOffset = userDataOffset + AlignBytesToPages(xvdHeader.UserDataLength);
        UInt64 batOffset = xvcHeaderOffset + AlignBytesToPages(xvdHeader.XVCLength);
        UInt64 dataOffset = batOffset + AlignBytesToPages(xvdHeader.DynHeaderLength);

        if (xvcHeaderOffset > long.MaxValue)
        {
            throw new InvalidOperationException("XVC header offset is too large.");
        }

        stream.Position = (long)xvcHeaderOffset;

        XvcHeader xvcHeader = XvcHeader.Read(stream);

        buildId = xvdHeader.PDUID;

        if (xvcHeader.NumberKeyIds != 1)
        {
            throw new InvalidOperationException("XVC header does not contain a single key ID.");
        }
        keyId = xvcHeader.KeyId[0];
    }

    internal static void ExtractFile(string packagePath, string fileName, out byte[]? fileContents)
    {
        fileContents = null;

        // Can only extract data from the unencrypted part of an XVC file.
        using FileStream stream = File.OpenRead(packagePath);

        XvdHeader xvdHeader = XvdHeader.Read(stream);

        UInt32 hashedPages = 0;

        if (!xvdHeader.FeatureBits.HasFlag(XvdFeatureBits.DataIntegrityDisabled))
        {
            hashedPages = BytesToPages(AlignBytesToPages(xvdHeader.DriveSize));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.UserDataLength));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.XVCLength));
            hashedPages += BytesToPages(AlignBytesToPages(xvdHeader.DynHeaderLength));
        }

        UInt64 unusedOffset = XvdHeader.Size;
        UInt64 embeddedOffset = unusedOffset + XVD_UNUSED_SPACE;
        UInt64 mutableXvcOffset = embeddedOffset + AlignBytesToPages(xvdHeader.EmbeddedXVDLength);
        UInt64 hashL3Offset = mutableXvcOffset + PagesToBytes(xvdHeader.MutableXVCPageCount);
        UInt64 hashL2Offset = hashL3Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 3));
        UInt64 hashL1Offset = hashL2Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 2));
        UInt64 hashL0Offset = hashL1Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 1));
        UInt64 userDataOffset = hashL0Offset + PagesToBytes(NumberOfHashPagesForLevel(hashedPages, 0));
        UInt64 xvcHeaderOffset = userDataOffset + AlignBytesToPages(xvdHeader.UserDataLength);
        UInt64 batOffset = xvcHeaderOffset + AlignBytesToPages(xvdHeader.XVCLength);
        UInt64 dataOffset = batOffset + AlignBytesToPages(xvdHeader.DynHeaderLength);

        if (xvcHeaderOffset > long.MaxValue)
        {
            throw new InvalidOperationException("XVC header offset is too large.");
        }

        stream.Position = (long)xvcHeaderOffset;

        XvcHeader xvcHeader = XvcHeader.Read(stream);

        var regions = new XvcRegion[xvcHeader.NumberRegions];
        for (int i = 0; i < regions.Length; ++i)
        {
            regions[i] = XvcRegion.Read(stream);
        }

        var segments = XvcSegment.Read(stream, xvcHeader.NumberSegments);

        var regionSpecifiers = new XvcRegionSpecifier[xvcHeader.NumberRegionSpecifiers];
        for (int i = 0; i < regionSpecifiers.Length; ++i)
        {
            regionSpecifiers[i] = XvcRegionSpecifier.Read(stream);
        }

        if (xvcHeader.Flags.HasFlag(XvcHeaderFlags.XVC_HEADER_FLAG_XTS_OFFSETS_CACHED))
        {
            using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
            {
                reader.ReadBytes((int)xvcHeader.NumberSegments * sizeof(UInt32));
            }
        }

        var xtsEntries = XvcXtsEntry.Read(stream, xvcHeader.NumberXtsEntries);

        stream.Position = (long)userDataOffset;
        var userDataHeader = UserDataHeader.Read(stream);
        UserDataPackageFile[] userDataFiles = [];
        if (userDataHeader != null)
        {
            var userDataPackageFilesHeader = UserDataPackageFilesHeader.Read(stream);
            userDataFiles = new UserDataPackageFile[userDataPackageFilesHeader.EntryCount];
            for (int i = 0; i < userDataFiles.Length; ++i)
            {
                userDataFiles[i] = UserDataPackageFile.Read(stream);
            }

            SegmentMetadata? metadata = null;

            var offset = userDataOffset + userDataHeader.HeaderLength;
            var bytes = GetPackageFileBytes(stream, userDataFiles, offset, "SegmentMetadata.bin");
            if (bytes != null)
            {
                metadata = SegmentMetadata.Read(bytes);

                for (int i = 0; i < metadata.SegmentCount; i++)
                {
                    var path = metadata.GetPath(i);
                    var size = metadata.GetSize(i);

                    if (string.Equals(path, fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        stream.Position = (long)XvcFile.PagesToBytes(segments[i].PageOffset);

                        using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
                        {
                            fileContents = reader.ReadBytes((int)size);
                        }
                    }
                }
            }
        }
    }

    public static uint GetPackageFileSize(UserDataPackageFile[] userDataFiles, string filePath)
    {
        if (userDataFiles == null)
        {
            return 0;
        }
        var entry = userDataFiles.FirstOrDefault(pf => pf.FilePath == filePath);
        if (entry == null)
        {
            return 0;
        }
        return entry.FileSize;
    }

    public static byte[]? GetPackageFileBytes(Stream stream, UserDataPackageFile[] userDataFiles, ulong baseOffset, string filePath)
    {
        var entry = userDataFiles.FirstOrDefault(pf => pf.FilePath == filePath);
        if (entry == null)
        {
            return null;
        }
        stream.Position = (long)(baseOffset + entry.Offset);
        if (stream != null)
        {
            var bytes = new byte[GetPackageFileSize(userDataFiles, filePath)];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var bytesRead = stream.Read(bytes, offset, bytes.Length - offset);
                offset += bytesRead;
                if (bytesRead == 0)
                {
                    throw new Exception("Stream ended prematurely");
                }
            }
            return bytes;
        }
        return null;
    }
}