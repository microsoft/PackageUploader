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

        public UploadConfigSchema UploadConfig { get; set; }

        public int MinutesToWaitForProcessing { get; set; }

        protected override void Validate(List<ValidationResult> validationResults)
        {
            if (string.IsNullOrWhiteSpace(BranchFriendlyName) && string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult("BranchFriendlyName or FlightName is required"));
            }

            if (!string.IsNullOrWhiteSpace(BranchFriendlyName) && !string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult("Only one BranchFriendlyName or FlightName is allowed"));
            }

            Validator.TryValidateObject(UploadConfig, new ValidationContext(UploadConfig), validationResults, true);
        }
    }
}
