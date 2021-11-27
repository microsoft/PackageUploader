// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class GenerateConfigTemplateOperation
    {
        private readonly ILogger<GenerateConfigTemplateOperation> _logger;
        private const int BufferSize = 8 * 1024;

        public GenerateConfigTemplateOperation(ILogger<GenerateConfigTemplateOperation> logger)
        {
            _logger = logger;
        }

        public async Task<int> RunAsync(Operations configOperation, CancellationToken ct)
        {
            try
            {
                _logger.LogDebug("GameStoreBroker is generating a config file template for {configOperation}.", configOperation);

                var assembly = Assembly.GetExecutingAssembly();
                var resources = assembly.GetManifestResourceNames();
                var resource = $"GameStoreBroker.Application.Templates.{configOperation}.json";

                using var resourceStream = assembly.GetManifestResourceStream(resource);
                if (resourceStream is null)
                {
                    _logger.LogInformation("Config file template for {configOperation} not found.", configOperation);
                    return -1;
                }
                else
                {
                    var destinationFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, $"{configOperation}.json"));
                    if (destinationFile.Exists)
                    {
                        _logger.LogInformation("Config file template already found for {configOperation}. It will be overwritten.", configOperation);
                    }
                    using var destinationFileStream = destinationFile.Open(FileMode.Create);
                    await CopyStream(resourceStream, destinationFileStream, ct).ConfigureAwait(false); ;

                    _logger.LogInformation("Config file template {destinationFile} generated.", destinationFile.Name);
                }

                return 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogTrace(e, "Exception thrown.");
                return -1;
            }
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
