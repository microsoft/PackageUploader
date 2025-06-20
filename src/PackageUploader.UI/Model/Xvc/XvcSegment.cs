// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public struct XvcSegment
{
    public UInt32 PageOffset;
    public UInt64 Hash;

    public static XvcSegment[] Read(Stream stream, uint count)
    {
        var segments = new XvcSegment[count];
        using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
        {
            for (uint i = 0; i < count; ++i)
            {
                segments[i].PageOffset = reader.ReadUInt32();
                segments[i].Hash = reader.ReadUInt64();
            }
        }
        return segments;
    }
}

