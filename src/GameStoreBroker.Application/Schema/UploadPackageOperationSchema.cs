// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class UploadPackageOperationSchema : BaseOperationSchema
    {
        public string BranchFriendlyName { get; set; }

        public string FlightName { get; set; }

        [Range(0, 360)]
        public int MinutesToWaitForProcessing { get; set; } = 30;

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if (string.IsNullOrWhiteSpace(BranchFriendlyName) && string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult("BranchFriendlyName or FlightName is required", new[] { nameof(BranchFriendlyName), nameof(FlightName) }));
            }

            if (!string.IsNullOrWhiteSpace(BranchFriendlyName) && !string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult("Only one BranchFriendlyName or FlightName is allowed", new[] { nameof(BranchFriendlyName), nameof(FlightName) }));
            }
        }
    }
}
