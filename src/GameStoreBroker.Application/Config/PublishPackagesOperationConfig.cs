// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal sealed class PublishPackagesOperationConfig : PackageBranchOperationConfig, IGameSubmissionConfiguration
    {
        internal override string GetOperationName() => "PublishPackages";

        public string DestinationSandboxName { get; set; }
        public int MinutesToWaitForPublishing { get; set; }
        public bool Retail { get; set; }
        public string CertificationNotes { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if ((!string.IsNullOrWhiteSpace(FlightName) || string.IsNullOrWhiteSpace(BranchFriendlyName) || string.IsNullOrWhiteSpace(DestinationSandboxName)) &&
                (string.IsNullOrWhiteSpace(FlightName) || !string.IsNullOrWhiteSpace(BranchFriendlyName) || !string.IsNullOrWhiteSpace(DestinationSandboxName)))
            {
                validationResults.Add(new ValidationResult($"{nameof(FlightName)} or ({nameof(BranchFriendlyName)} and {nameof(DestinationSandboxName)}) field is required.",
                    new[] { nameof(FlightName), nameof(BranchFriendlyName), nameof(DestinationSandboxName) }));
            }
        }
    }
}
