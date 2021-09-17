// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class ImportPackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override string GetOperationName() => "ImportPackages";

        public string MarketGroupId { get; set; } = null;

        public string DestinationBranchFriendlyName { get; set; }
        public string DestinationFlightName { get; set; }

        public bool Overwrite { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            base.Validate(validationResults);
            if (string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && string.IsNullOrWhiteSpace(DestinationFlightName))
            {
                validationResults.Add(new ValidationResult($"{nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is required.", new[] { nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName) }));
            }

            if (!string.IsNullOrWhiteSpace(DestinationBranchFriendlyName) && !string.IsNullOrWhiteSpace(DestinationFlightName))
            {
                validationResults.Add(new ValidationResult($"Only one {nameof(DestinationBranchFriendlyName)} or {nameof(DestinationFlightName)} field is allowed.", new[] { nameof(DestinationBranchFriendlyName), nameof(DestinationFlightName) }));
            }
        }
    }
}
