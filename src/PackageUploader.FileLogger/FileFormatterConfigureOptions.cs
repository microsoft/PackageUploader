// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace PackageUploader.FileLogger;

/// <summary>
/// Configures a FileFormatterOptions object from an IConfiguration.
/// </summary>
/// <remarks>
/// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
/// to be trimmed. This improves app size and startup.
/// </remarks>
public class FileFormatterConfigureOptions : IConfigureOptions<FileFormatterOptions>
{

    private readonly IConfiguration _configuration;

    public FileFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        _configuration = providerConfiguration.GetFormatterOptionsSection();
    }

    public void Configure(FileFormatterOptions options) => options.Configure(_configuration);
}