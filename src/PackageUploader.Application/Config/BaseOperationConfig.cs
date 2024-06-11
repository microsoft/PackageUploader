// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config;

internal abstract class BaseOperationConfig : IValidatableObject
{
    internal abstract string GetOperationName();

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

    protected virtual void Validate(List<ValidationResult> validationResults)
    {
    }

    private void ValidateBase(List<ValidationResult> validationResults)
    {
        var operationName = GetOperationName();
        if (!string.Equals(operationName, OperationName, StringComparison.OrdinalIgnoreCase))
        {
            validationResults.Add(new ValidationResult($"{nameof(OperationName)} field is not {operationName}.", [nameof(OperationName)]));
        }

        if (string.IsNullOrWhiteSpace(ProductId) && string.IsNullOrWhiteSpace(BigId))
        {
            validationResults.Add(new ValidationResult($"{nameof(ProductId)} or {nameof(BigId)} field is required.", [nameof(ProductId), nameof(BigId)]));
        }

        if (!string.IsNullOrWhiteSpace(ProductId) && !string.IsNullOrWhiteSpace(BigId))
        {
            validationResults.Add(new ValidationResult($"Only one {nameof(ProductId)} or {nameof(BigId)} field is allowed.", [nameof(ProductId), nameof(BigId)]));
        }
    }
}