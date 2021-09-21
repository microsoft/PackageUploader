using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Config
{
    internal sealed class PublishPackagesOperationConfig : PackageBranchOperationConfig
    {
        internal override string GetOperationName() => "PublishPackages";

        public string DestinationSandboxName { get; set; }
        public int MinutesToWaitForPublishing { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            if (string.IsNullOrWhiteSpace(FlightName) || (string.IsNullOrWhiteSpace(BranchFriendlyName) && string.IsNullOrWhiteSpace(DestinationSandboxName)))
            {
                validationResults.Add(new ValidationResult($"{nameof(FlightName)} or ({nameof(BranchFriendlyName)} and {nameof(DestinationSandboxName)}) field is required.", new[] { nameof(FlightName), nameof(BranchFriendlyName), nameof(DestinationSandboxName) }));
            }

            if ((!string.IsNullOrWhiteSpace(DestinationSandboxName) || !string.IsNullOrWhiteSpace(BranchFriendlyName)) && !string.IsNullOrWhiteSpace(FlightName))
            {
                validationResults.Add(new ValidationResult($"Only one {nameof(BranchFriendlyName)} or {nameof(FlightName)} field is allowed. {nameof(DestinationSandboxName)} must be provided with {nameof(BranchFriendlyName)}", new[] { nameof(BranchFriendlyName), nameof(FlightName), nameof(DestinationSandboxName) }));
            }
        }
    }
}
