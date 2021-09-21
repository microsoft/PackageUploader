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
        public string DestinationFlightName { get; set; }
        public int MinutesToWaitForPublishing { get; set; }

        protected override void Validate(IList<ValidationResult> validationResults)
        {
            base.Validate(validationResults);

            if (string.IsNullOrWhiteSpace(DestinationSandboxName) && string.IsNullOrWhiteSpace(DestinationFlightName))
            {
                validationResults.Add(new ValidationResult($"{nameof(DestinationSandboxName)} or {nameof(DestinationFlightName)} field is required.", new[] { nameof(DestinationSandboxName), nameof(DestinationFlightName) }));
            }

            if (!string.IsNullOrWhiteSpace(DestinationSandboxName) && !string.IsNullOrWhiteSpace(DestinationFlightName))
            {
                validationResults.Add(new ValidationResult($"Only one {nameof(DestinationSandboxName)} or {nameof(DestinationFlightName)} field is allowed.", new[] { nameof(DestinationSandboxName), nameof(DestinationFlightName) }));
            }
        }
    }
}
