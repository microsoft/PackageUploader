// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;
public struct XvcXtsEntry
{
    public UInt32 PageOffset;
    public UInt32 XtsOffset;

    public static XvcXtsEntry[] Read(Stream stream, uint count)
    {
        var entries = new XvcXtsEntry[count];
        using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
        {
            for (uint i = 0; i < count; ++i)
            {
                entries[i].PageOffset = reader.ReadUInt32();
                entries[i].XtsOffset = reader.ReadUInt32();
            }
        }
        return entries;
    }
}