// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal class UploadUwpPackageOperation : Operation
    {
        private readonly IHost _host;
        private readonly ILogger<UploadUwpPackageOperation> _logger;

        public UploadUwpPackageOperation(IHost host, Options options) : base(host, options)
        {
            _host = host;
            _logger = _host.Services.GetRequiredService<ILogger<UploadUwpPackageOperation>>();
        }

        protected override async Task ProcessAsync(CancellationToken ct)
        {
            using var scope = _host.Services.CreateScope();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();

            _logger.LogInformation("Starting UploadUwpPackage operation.");

            var schema = await GetSchemaAsync<UploadUwpPackageOperationSchema>(ct).ConfigureAwait(false);
            var aadAuthInfo = GetAadAuthInfo(schema.AadAuthInfo);
            var accessTokenProvider = new AccessTokenProvider(aadAuthInfo);

            var product = await new ProductService(storeBroker, accessTokenProvider).GetProductAsync(schema, ct).ConfigureAwait(false);
            var packageFilePath = new FileInfo(schema.PackageFilePath);
            await storeBroker.UploadUwpPackageAsync(accessTokenProvider, product, packageFilePath, ct);
        }
    }
}