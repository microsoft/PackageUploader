// Copyright (C) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.Api;
using GameStoreBroker.Application.Schema;
using GameStoreBroker.Client.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.Application.Commands
{
    internal abstract class Action
    {
        private readonly Options _options;
        private readonly ILogger<Action> _logger;

        protected Action(IHost host, Options options)
        {
            _options = options;
            _logger = host.Services.GetRequiredService<ILogger<Action>>();
        }

        protected async Task<T> GetSchema<T>() where T : BaseOperationSchema
        {
            var schema = await new SchemaReader<T>().DeserializeFile(_options.ConfigFile).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                schema.AadAuthInfo.ClientSecret = _options.ClientSecret;
            }

            return schema;
        }

        public async Task<int> Run(CancellationToken ct)
        {
            try
            {
                _logger.LogDebug("GameStoreBroker is running.");
                await Process(ct).ConfigureAwait(false);
                _logger.LogInformation("GameStoreBroker has finished running.");
                return 0;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Operation cancelled.");
                return 1;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown.");
                return 1;
            }
        }

        protected abstract Task Process(CancellationToken ct);
    }
}