// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class GenerateConfigTemplateOperation : Operation
    {
        private const int BufferSize = 8 * 1024;
        private readonly GenerateConfigTemplateOperationConfig _config;

        public GenerateConfigTemplateOperation(IOptions<GenerateConfigTemplateOperationConfig> config, ILogger<GenerateConfigTemplateOperation> logger) : base(logger)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            var configOperation = _config.GenerateConfigTemplateOperationName;
            _logger.LogDebug("GameStoreBroker is generating a config file template for {configOperation}.", configOperation);

            var assembly = Assembly.GetExecutingAssembly();
            var resource = $"GameStoreBroker.Application.Templates.{configOperation}.json";

            using var resourceStream = assembly.GetManifestResourceStream(resource);
            if (resourceStream is null)
            {
                _logger.LogError("Config file template for {configOperation} not found.", configOperation);
            }
            else
            {
                var generate = true;
                var destinationFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, $"{configOperation}.json"));
                if (destinationFile.Exists)
                {
                    if (_config.Overwrite)
                    {
                        _logger.LogWarning("Config file template {destinationFile} will be overwritten.", destinationFile.Name);
                    }
                    else
                    {
                        generate = false;
                        _logger.LogWarning("Config file template {destinationFile} already exists. No template will be generated.", destinationFile.Name);
                    }
                }
                if (generate)
                {
                    await GenerateConfigFile(resourceStream, destinationFile, ct).ConfigureAwait(false);
                    _logger.LogInformation("Config file template {destinationFile} generated.", destinationFile.Name);
                }
            }
        }

        private static async ValueTask GenerateConfigFile(Stream originStream, FileInfo destinationFile, CancellationToken ct)
        {
            using var destinationFileStream = destinationFile.Open(FileMode.Create);
            await CopyStream(originStream, destinationFileStream, ct).ConfigureAwait(false); ;
        }

        private static async ValueTask CopyStream(Stream input, Stream output, CancellationToken ct)
        {
            byte[] buffer = new byte[BufferSize];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, len), ct).ConfigureAwait(false); ;
            }
        }
    }
}
