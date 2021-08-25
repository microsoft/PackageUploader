// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal abstract class Operation
    {
        private readonly Options _options;
        private readonly ILogger<Operation> _logger;

        private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions();

        protected Operation(IHost host, Options options)
        {
            _options = options;
            _logger = host.Services.GetRequiredService<ILogger<Operation>>();
        }

        protected async Task<T> GetSchemaAsync<T>(CancellationToken ct) where T : BaseOperationSchema
        {
            if (!_options.ConfigFile.Exists)
            {
                throw new FileNotFoundException("ConfigFile does not exist.", _options.ConfigFile.FullName);
            }

            var schema = await DeserializeJsonFileAsync<T>(_options.ConfigFile.FullName, ct).ConfigureAwait(false);
            ValidateSchema(schema);

            if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                schema.AadAuthInfo.ClientSecret = _options.ClientSecret;
            }

            return schema;
        }

        protected static AadAuthInfo GetAadAuthInfo(AadAuthInfoSchema aadAuthInfoSchema)
        {
            if (aadAuthInfoSchema == null)
            {
                throw new ArgumentNullException(nameof(aadAuthInfoSchema));
            }

            if (string.IsNullOrWhiteSpace(aadAuthInfoSchema.ClientSecret))
            {
                throw new Exception("ClientSecret not provided.");
            }

            var aadAuthInfo = new AadAuthInfo
            {
                TenantId = aadAuthInfoSchema.TenantId,
                ClientId = aadAuthInfoSchema.ClientId,
                ClientSecret = aadAuthInfoSchema.ClientSecret,
            };
            return aadAuthInfo;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                _logger.LogDebug("GameStoreBroker is running.");
                await ProcessAsync(ct).ConfigureAwait(false);
                return 0;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Operation cancelled.");
                return 1;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogTrace(e, "Exception thrown.");
                return 1;
            }
            finally
            {
                _logger.LogInformation("GameStoreBroker has finished running.");
            }
        }

        protected abstract Task ProcessAsync(CancellationToken ct);

        private static async Task<T> DeserializeJsonFileAsync<T>(string fileName, CancellationToken ct)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            await using var openStream = File.OpenRead(fileName);
            var deserializedObject = await JsonSerializer.DeserializeAsync<T>(openStream, DefaultJsonSerializerOptions, ct).ConfigureAwait(false);
            return deserializedObject;
        }

        private static void ValidateSchema<T>(T schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (!schema.IsValid(out var errorMessages))
            {
                throw new Exception("Errors found in json file: " + string.Join(", ", errorMessages) + ".");
            }
        }
    }
}