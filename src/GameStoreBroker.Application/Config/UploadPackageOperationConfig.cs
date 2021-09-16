// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class UploadPackageOperationConfig : PackageBranchOperationConfig
    {
        [Required]
        public string MarketGroupId { get; set; } = "default";

        [Range(0, 360)]
        public int MinutesToWaitForProcessing { get; set; } = 30;
    }
}
