﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations
{
    internal class GenerateConfigTemplateOperation : Operation
    {
        private const int BufferSize = 8 * 1024;
        private readonly InvocationContext _invocationContext;

        public GenerateConfigTemplateOperation(ILogger<GenerateConfigTemplateOperation> logger, InvocationContext invocationContext) : base(logger)
        {
            _invocationContext = invocationContext;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            var operationName = _invocationContext.ParseResult.RootCommandResult.Children[0].Symbol.Name;
            var overwrite = _invocationContext.ParseResult.ValueForOption(Program.OverwriteOption);
            var destinationFile = _invocationContext.ParseResult.ValueForOption(Program.NewConfigFileOption)?? 
                new FileInfo(Path.Combine(Environment.CurrentDirectory, $"{operationName}.json"));

            _logger.LogDebug("Generating config file template for {configOperation} operation.", operationName);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"PackageUploader.Application.Templates.{operationName}.json";

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is null)
            {
                _logger.LogError("Config file template for {configOperation} not found.", operationName);
            }
            else
            {
                var generate = true;
                if (destinationFile.Exists)
                {
                    if (overwrite)
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
            await CopyStream(originStream, destinationFileStream, ct).ConfigureAwait(false);
        }

        private static async ValueTask CopyStream(Stream input, Stream output, CancellationToken ct)
        {
            byte[] buffer = new byte[BufferSize];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, len), ct).ConfigureAwait(false);
            }
        }
    }
}