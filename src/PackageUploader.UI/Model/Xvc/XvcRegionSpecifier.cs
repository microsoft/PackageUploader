// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using System.IO;
using System.Text;

namespace PackageUploader.UI.Model;

public class XvcRegionSpecifier
{
    public const int XVC_REGION_SPECIFIER_KEY_LENGTH = 64;
    public const int XVC_REGION_SPECIFIER_VALUE_LENGTH = 128;

    public const string Languages = "Languages";
    public const string Devices = "Devices";
    public const string Tags = "Tags";
    public const string ContentTypes = "ContentTypes";

    public UInt32 RegionId;
    public UInt32 Flags;
    public string Key = string.Empty; // XVC_REGION_SPECIFIER_KEY_LENGTH
    public string Value = string.Empty; // XVC_REGION_SPECIFIER_VALUE_LENGTH

    public static XvcRegionSpecifier Read(Stream stream)
    {
        using (var reader = new BinaryReader(stream, Encoding.Unicode, true))
        {
            var specifier = new XvcRegionSpecifier();
            specifier.RegionId = reader.ReadUInt32();
            specifier.Flags = reader.ReadUInt32();
            specifier.Key = reader.ReadNullTerminatedString(XVC_REGION_SPECIFIER_KEY_LENGTH);
            specifier.Value = reader.ReadNullTerminatedString(XVC_REGION_SPECIFIER_VALUE_LENGTH);
            return specifier;
        }
    }
}

