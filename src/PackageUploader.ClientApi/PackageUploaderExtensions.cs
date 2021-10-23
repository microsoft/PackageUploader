// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Xfus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PackageUploader.ClientApi
{
    public static class IngestionExtensions
    {
        public static void AddPackageUploaderService(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IPackageUploaderService, PackageUploaderService>();
            services.AddIngestionService(config);
            services.AddAccessTokenProvider(config);
            services.AddXfusService(config);
        }
    }
}