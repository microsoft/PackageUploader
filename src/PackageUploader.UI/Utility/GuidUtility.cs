// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.Utility;

public static class GuidUtility
{
    public static Guid ReadGuid(this BinaryReader reader)
    {
        return new Guid(reader.ReadBytes(16));
    }

    public static Guid[] ReadGuids(this BinaryReader reader, int count)
    {
        var output = new Guid[count];
        for (int i = 0; i < count; ++i)
        {
            output[i] = reader.ReadGuid();
        }
        return output;
    }
}
