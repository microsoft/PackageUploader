// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal abstract class UploadPackageOperationConfig : PackageBranchOperationConfig
    {
        [Required]
        public string MarketGroupId { get; set; } = "default";

        [Range(0, 360)]
        public int MinutesToWaitForProcessing { get; set; } = 30;

        [Required]
        public string PackageFilePath { get; set; }
    }
}
