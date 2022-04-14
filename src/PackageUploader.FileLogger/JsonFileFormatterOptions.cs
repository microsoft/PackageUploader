// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace PackageUploader.FileLogger;

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