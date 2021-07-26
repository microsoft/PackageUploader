// Copyright (C) Microsoft. All rights reserved.

using System;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Commands
{
    internal class UploadXboxPackage : Action
    {
        private readonly IHost _host;

        public UploadXboxPackage(IHost host, Options options) : base(host, options)
        {
            _host = host;
        }

        protected override async Task Process(CancellationToken ct)
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UploadXboxPackage>>();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();

            logger.LogInformation("Starting UploadXboxPackage process");
            throw new NotImplementedException();
        }
    }
}