// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public enum UserDataType
{
    XvdUserDataPackageFiles = 0,
}

public class UserDataHeader
{
    public const UInt32 XVD_USER_DATA_VERSION = 0x1;

    public UInt32 HeaderLength;
    public UInt32 HeaderVersion;
    public UserDataType DataType;
    public UInt32 DataLength;

    public static UserDataHeader? Read(Stream stream)
    {
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            var header = new UserDataHeader();
            header.HeaderLength = reader.ReadUInt32();
            if (header.HeaderLength < 16)
                return null;
            header.HeaderVersion = reader.ReadUInt32();
            header.DataType = (UserDataType)reader.ReadUInt32();
            if (header.DataType != UserDataType.XvdUserDataPackageFiles)
                return null;
            header.DataLength = reader.ReadUInt32();
            reader.ReadBytes((int)header.HeaderLength - 16);
            return header;
        }
    }
}