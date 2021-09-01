// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace GameStoreBroker.Application.Extensions
{
    internal static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions()
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string ToJson<T>(this T value, JsonSerializerOptions jsonSerializerOptions = null) where T : class
        {
            if (value == null)
            {
                return "null";
            }

            try
            {
                var serializedObject = JsonSerializer.Serialize(value, jsonSerializerOptions ?? DefaultJsonSerializerOptions);
                return serializedObject;
            }
            catch (Exception ex)
            {
                return $"Could not serialize object to json - {ex.Message}";
            }
        }

        public static bool IsValid<T>(this T objectToValidate, out IEnumerable<string> validationErrors)
        {
            validationErrors = null;
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(objectToValidate, new ValidationContext(objectToValidate), validationResults, true);

            validationErrors = validationResults.Select(x => x.ErrorMessage);
            return isValid;
        }
    }
}
