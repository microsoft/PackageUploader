// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace PackageUploader.FileLogger
{
    /// <summary>
    /// Configures a FileLoggerOptions object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    internal sealed class FileLoggerConfigureOptions : IConfigureOptions<FileLoggerOptions>
    {
        private readonly IConfiguration _configuration;

        public FileLoggerConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(FileLoggerOptions options) => _configuration.Bind(options);
    }
}