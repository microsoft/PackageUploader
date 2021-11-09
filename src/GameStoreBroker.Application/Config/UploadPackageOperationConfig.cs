// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal abstract class UploadPackageOperationConfig : PackageBranchOperationConfig
    {
        public string MarketGroupName { get; set; } = "default";

        [Range(0, 360)]
        public int MinutesToWaitForProcessing { get; set; } = 30;

        [Required]
        public string PackageFilePath { get; set; }

        public GamePackageDate AvailabilityDate { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if (string.IsNullOrWhiteSpace(MarketGroupName))
            {
                MarketGroupName = "default";
            }
        }
    }
}
