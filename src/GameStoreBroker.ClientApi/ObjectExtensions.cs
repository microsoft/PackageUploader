// Copyright (C) Microsoft. All rights reserved.

using System;
using Newtonsoft.Json;

namespace GameStoreBroker.ClientApi
{
    internal static class ObjectExtensions
    {
        public static string ToJson<T>(this T value, Formatting formatting = Formatting.None, JsonSerializerSettings settings = null) where T : class
        {
            if (value == null)
            {
                return "null";
            }

            try
            {
                var json = JsonConvert.SerializeObject(value, formatting, settings);
                return json;
            }
            catch (Exception ex)
            {
                return $"Could not serialize object to json - {ex.Message}";
            }
        }
    }
}
