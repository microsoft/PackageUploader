// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;
using GameStoreBroker.ClientApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal sealed class PublishPackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.PublishPackages;

        public string DestinationSandboxName { get; set; }
        public int MinutesToWaitForPublishing { get; set; }
        public GamePublishConfiguration PublishConfiguration { get; set; }

        private const string RetailSandboxName = "RETAIL";

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if ((!string.IsNullOrWhiteSpace(FlightName) || string.IsNullOrWhiteSpace(BranchFriendlyName) || string.IsNullOrWhiteSpace(DestinationSandboxName)) &&
                (string.IsNullOrWhiteSpace(FlightName) || !string.IsNullOrWhiteSpace(BranchFriendlyName) || !string.IsNullOrWhiteSpace(DestinationSandboxName)))
            {
                validationResults.Add(new ValidationResult($"{nameof(FlightName)} or ({nameof(BranchFriendlyName)} and {nameof(DestinationSandboxName)}) field is required.",
                    new[] { nameof(FlightName), nameof(BranchFriendlyName), nameof(DestinationSandboxName) }));
            }

            if (!string.IsNullOrWhiteSpace(DestinationSandboxName) && DestinationSandboxName.Equals(RetailSandboxName, StringComparison.OrdinalIgnoreCase))
            {
                validationResults.Add(new ValidationResult($"Publishing packages to {RetailSandboxName} sandbox is not permitted through this tool.", 
                    new[] { nameof(DestinationSandboxName) }));
            }
        }
    }
}
