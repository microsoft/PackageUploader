// Copyright (C) Microsoft. All rights reserved.

using System;
using System.Text.Json;

namespace GameStoreBroker.Application
{
    internal static class ObjectExtensions
    {
        public static string ToJson<T>(this T value, JsonSerializerOptions jsonSerializerOptions = null) where T : class
        {
            if (value == null)
            {
                return "null";
            }

            try
            {
                var serializedObject = JsonSerializer.Serialize(value, jsonSerializerOptions);
                return serializedObject;
            }
            catch (Exception ex)
            {
                return $"Could not serialize object to json - {ex.Message}";
            }
        }
    }
}
