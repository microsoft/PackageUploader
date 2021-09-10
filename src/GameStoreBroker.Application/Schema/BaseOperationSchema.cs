// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class BaseOperationSchema : IValidatableObject
    {
        protected abstract string GetOperationName();

        [Required]
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

        protected virtual void Validate(IList<ValidationResult> validationResults)
        {
        }

        private void ValidateBase(IList<ValidationResult> validationResults)
        {
            var operationName = GetOperationName();
            if (!string.Equals(operationName, OperationName))
            {
                validationResults.Add(new ValidationResult($"{nameof(OperationName)} field is not {operationName}.", new [] { nameof(OperationName) }));
            }

            if (string.IsNullOrWhiteSpace(ProductId) && string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult($"{nameof(ProductId)} or {nameof(BigId)} field is required.", new[] { nameof(ProductId), nameof(BigId) }));
            }

            if (!string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult($"Only one {nameof(ProductId)} or {nameof(BigId)} field is allowed.", new[] { nameof(ProductId), nameof(BigId) }));
            }
        }
    }
}
