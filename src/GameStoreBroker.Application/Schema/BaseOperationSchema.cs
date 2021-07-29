// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class BaseOperationSchema : IValidatableObject
    {
        public abstract string GetOperationName();

        [Required(ErrorMessage = "operationName is required")]
        [JsonPropertyName("operationName")]
        public string OperationName { get; set; }
        
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }
        
        [JsonPropertyName("bigId")]
        public string BigId { get; set; }
        
        [Required(ErrorMessage = "aadAuthInfo is required")]
        [JsonPropertyName("aadAuthInfo")]
        public AadAuthInfoSchema AadAuthInfo { get; set; }

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
                validationResults.Add(new ValidationResult($"operationName is not {operationName}"));
            }

            if (string.IsNullOrWhiteSpace(ProductId) && string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult("productId or bigId is required"));
            }

            if (!string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(BigId))
            {
                validationResults.Add(new ValidationResult("Only one productId or bigId is allowed"));
            }

            Validator.TryValidateObject(AadAuthInfo, new ValidationContext(AadAuthInfo), validationResults, true);
        }
    }
}
