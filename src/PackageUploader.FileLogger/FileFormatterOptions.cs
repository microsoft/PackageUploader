// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace PackageUploader.FileLogger;

/// <summary>
/// Options for the built-in file log formatter.
/// </summary>
public class FileFormatterOptions
{
    /// <summary>
    /// Includes scopes when <see langword="true" />.
    /// </summary>
    public bool IncludeScopes { get; set; }

    /// <summary>
    /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
    /// </summary>
    public string TimestampFormat { get; set; }

    /// <summary>
    /// Gets or sets indication whether or not UTC timezone should be used to for timestamps in logging messages. Defaults to <c>false</c>.
    /// </summary>
    public bool UseUtcTimestamp { get; set; }

    internal virtual void Configure(IConfiguration configuration) => configuration.Bind(this);
}