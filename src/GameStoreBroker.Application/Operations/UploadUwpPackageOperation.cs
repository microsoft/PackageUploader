// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Schema;
using GameStoreBroker.Application.Services;
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
            _logger.LogInformation("Starting UploadUwpPackage operation.");

            var schema = await GetSchemaAsync<UploadUwpPackageOperationSchema>(ct).ConfigureAwait(false);

            using var scope = _host.Services.CreateScope();
            var storeBroker = scope.ServiceProvider.GetRequiredService<IGameStoreBrokerService>();
            var accessTokenProvider = scope.ServiceProvider.GetRequiredService<IAccessTokenProvider>();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            
            var product = await productService.GetProductAsync(schema, ct).ConfigureAwait(false);
            var packageBranch = await productService.GetGamePackageBranch(product, schema, ct).ConfigureAwait(false);

            var packageFilePath = new FileInfo(schema.PackageFilePath);
            await storeBroker.UploadUwpPackageAsync(accessTokenProvider, product, packageBranch, packageFilePath, ct);
        }
    }
}