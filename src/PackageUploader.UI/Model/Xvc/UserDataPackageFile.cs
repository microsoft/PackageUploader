// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public class UserDataPackageFile
{
    public string FilePath = string.Empty;
    public UInt32 FileSize;
    public UInt32 Offset; // Relative to start of UserDataPackageFilesHeader

    public static UserDataPackageFile Read(Stream stream)
    {
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            var entry = new UserDataPackageFile();
            entry.FilePath = reader.ReadNullTerminatedString(260);
            entry.FileSize = reader.ReadUInt32();
            entry.Offset = reader.ReadUInt32();
            return entry;
        }
    }
}