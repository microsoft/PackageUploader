// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.Reflection;

namespace PackageUploader.Application.Test.Extensions
{
    /// <summary>
    /// Extension methods for System.CommandLine.Option<T> that are useful for testing
    /// </summary>
    internal static class OptionExtensions
    {
        /// <summary>
        /// Gets the default value from an Option using reflection (for testing purposes)
        /// </summary>
        public static T? GetDefaultValue<T>(this Option<T> option)
        {
            // Use the default value factory method to get the default value
            var defaultValueSource = option.GetType().GetProperty("DefaultValueFactory", 
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(option);

            if (defaultValueSource != null)
            {
                // Invoke the default value factory method to get the default value
                var method = defaultValueSource.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    return (T?)method.Invoke(defaultValueSource, null);
                }
            }

            // Default value can be stored as a simple property as well
            var defValProp = option.GetType().GetProperty("DefaultValue",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(option);

            if (defValProp != null)
            {
                return (T)defValProp;
            }

            return default;
        }
    }
}