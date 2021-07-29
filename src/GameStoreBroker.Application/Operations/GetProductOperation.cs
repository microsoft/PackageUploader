// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class GetProductOperation : Operation
    {
        private readonly IHost _host;

        public GetProductOperation(IHost host, Options options) : base(host, options)
        {
            _host = host;
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GetProductOperation>>();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();

            logger.LogInformation("Starting GetProduct operation.");

            var schema = await GetSchemaAsync<GetProductOperationSchema>(ct).ConfigureAwait(false);
            var aadAuthInfo = GetAadAuthInfo(schema.AadAuthInfo);

            GameProduct product;
            if (!string.IsNullOrWhiteSpace(schema.BigId))
            {
                product = await storeBroker.GetProductByBigIdAsync(aadAuthInfo, schema.BigId, ct).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(schema.ProductId))
            {
                product = await storeBroker.GetProductByProductIdAsync(aadAuthInfo, schema.ProductId, ct).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("BigId or ProductId needed.");
            }

            logger.LogInformation("Product: {product}", product.ToJson());
        }
    }
}