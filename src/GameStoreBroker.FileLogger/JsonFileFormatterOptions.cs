// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace GameStoreBroker.FileLogger
{
    /// <summary>
    /// Options for the built-in json file log formatter.
    /// </summary>
    public class JsonFileFormatterOptions : FileFormatterOptions
    {
        /// <summary>
        /// Gets or sets JsonWriterOptions.
        /// </summary>
        public JsonWriterOptions JsonWriterOptions { get; set; }
    }
}
