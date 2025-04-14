// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

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
}