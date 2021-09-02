// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;

namespace GameStoreBroker.ClientApi.Extensions
{
    internal static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new ()
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static string ToJson<T>(this T value, JsonSerializerOptions jsonSerializerOptions = null) where T : class
        {
            if (value is null)
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
    }
}
