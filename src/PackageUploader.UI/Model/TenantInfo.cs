// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.Model
{
    public class TenantInfo
    {
        public string? Name { get; set; }
        public string? Id { get; set; }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }
}