// Copyright (C) Microsoft. All rights reserved.

using GameStoreBroker.Client.Schema;
using GameStoreBroker.ClientApi;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Commands
{
    internal class GetProduct : CommandAction
    {
        private readonly IHost _host;

        public GetProduct(IHost host, Options options) : base(host, options)
        {
            _host = host;
        }

        protected override async Task Process(CancellationToken ct)
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GetProduct>>();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();

            logger.LogInformation("Starting GetProduct process");

            var schema = await GetSchema<GetProductOperationSchema>().ConfigureAwait(false);

            GameProduct product;
            if (!string.IsNullOrWhiteSpace(schema.BigId))
            {
                product = await storeBroker.GetProductByBigId(schema.AadAuthInfo, schema.BigId).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(schema.ProductId))
            {
                product = await storeBroker.GetProductByProductId(schema.AadAuthInfo, schema.ProductId).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("BigId or ProductId needed.");
            }

            logger.LogInformation("Product: {product}", product.ToJson());
        }
    }
}