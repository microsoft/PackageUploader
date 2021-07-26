using System;
using Newtonsoft.Json;

namespace GameStoreBroker.Application
{
    public static class ObjectExtensions
    {
        public static string ToJson<T>(this T value, Formatting formatting = Formatting.None) where T : class
        {
            if (value == null)
            {
                return "null";
            }

            try
            {
                var json = JsonConvert.SerializeObject(value, formatting);
                return json;
            }
            catch (Exception ex)
            {
                return $"Could not serialize object to json - {ex.Message}";
            }
        }
    }
}
