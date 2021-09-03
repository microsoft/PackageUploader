// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class BaseOperationSchema : IValidatableObject
    {
        public abstract string GetOperationName();

        [Required(ErrorMessage = "operationName is required")]
        public string OperationName { get; set; }
        
        public string ProductId { get; set; }
        
        public string BigId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();
            Validate(validationResults);
            ValidateBase(validationResults);
            return validationResults;
        }

        protected virtual void Validate(List<ValidationResult> validationResults)
        {
        }

        protected void ValidateBase(List<ValidationResult> validationResults)
        {
            var operationName = GetOperationName();
            if (!string.Equals(operationName, OperationName))
            {
                validationResults.Add(new ValidationResult($"operationName is not {operationName}", new [] { nameof(OperationName) }));
            }

            if (string.IsNullOrWhiteSpace(ProductId) && string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult("ProductId or BigId is required", new[] { nameof(ProductId), nameof(BigId) }));
            }

            if (!string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult("Only one ProductId or BigId is allowed", new[] { nameof(ProductId), nameof(BigId) }));
            }
        }
    }
}
