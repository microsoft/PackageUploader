// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public class UserDataPackageFilesHeader
{
    public UInt32 Version;
    public string PackageFullName = string.Empty;
    public UInt32 EntryCount;

    public static UserDataPackageFilesHeader Read(Stream stream)
    {
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            var header = new UserDataPackageFilesHeader();
            header.Version = reader.ReadUInt32();
            header.PackageFullName = reader.ReadNullTerminatedString(260);
            header.EntryCount = reader.ReadUInt32();
            return header;
        }
    }
}