// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config
{
    internal abstract class PackageBranchOperationConfig : BaseOperationConfig
    {
        public string BranchFriendlyName { get; set; }
        public string FlightName { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if (string.IsNullOrWhiteSpace(BranchFriendlyName) && string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult($"{nameof(BranchFriendlyName)} or {nameof(FlightName)} field is required.", new[] { nameof(BranchFriendlyName), nameof(FlightName) }));
            }

            if (!string.IsNullOrWhiteSpace(BranchFriendlyName) && !string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult($"Only one {nameof(BranchFriendlyName)} or {nameof(FlightName)} field is allowed.", new[] { nameof(BranchFriendlyName), nameof(FlightName) }));
            }
        }
    }
}
