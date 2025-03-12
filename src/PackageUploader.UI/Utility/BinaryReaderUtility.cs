// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace PackageUploader.UI.Utility;

public static class BinaryReaderUtility
{
    public static string ReadNullTerminatedString(this BinaryReader reader, int charLength)
    {
        var str = Encoding.Unicode.GetString(reader.ReadBytes(charLength * 2));
        var index = str.IndexOf('\0');
        return index >= 0 ? str[..index] : str;
    }
}