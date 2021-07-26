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
    internal class UploadPcPackage : Action
    {
        private readonly IHost _host;

        public UploadPcPackage(IHost host, Options options) : base(host, options)
        {
            _host = host;
        }

        protected override async Task Process(CancellationToken ct)
        {
            using var scope = _host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UploadPcPackage>>();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();

            logger.LogInformation("Starting UploadPcPackage process");
            throw new NotImplementedException();
        }
    }
}