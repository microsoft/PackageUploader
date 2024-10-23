// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace PackageUploader.FileLogger;

/// <summary>
/// Options for a <see cref="FileLogger"/>.
/// </summary>
public class FileLoggerOptions
{
    public string FormatterName { get; set; }

    internal virtual void Configure(IConfiguration configuration) => configuration.Bind(this);
}