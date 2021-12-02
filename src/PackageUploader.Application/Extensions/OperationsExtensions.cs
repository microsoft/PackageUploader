// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PackageUploader.Application.Config;
using PackageUploader.Application.Operations;

namespace PackageUploader.Application.Extensions
{
    internal static class OperationsExtensions
    {
        public static IServiceCollection AddOperations(this IServiceCollection services, IConfiguration config)
        {
            services.AddOperation<GetProductOperation, GetProductOperationConfig>(config);
            services.AddOperation<UploadUwpPackageOperation, UploadUwpPackageOperationConfig>(config);
            services.AddOperation<UploadXvcPackageOperation, UploadXvcPackageOperationConfig>(config);
            services.AddOperation<RemovePackagesOperation, RemovePackagesOperationConfig>(config);
            services.AddOperation<ImportPackagesOperation, ImportPackagesOperationConfig>(config);
            services.AddOperation<PublishPackagesOperation, PublishPackagesOperationConfig>(config);
            services.AddOperation<NewConfigOperation>();
            services.AddOperation<ValidateConfigOperation>();

            return services;
        }

        private static void AddOperation<TOperation, TConfig>(this IServiceCollection services, IConfiguration config) 
            where TOperation : Operation where TConfig : class
        {
            services.AddOperation<TOperation>();
            services.AddOptions<TConfig>().Bind(config).ValidateDataAnnotations();
        }

        private static void AddOperation<TOperation>(this IServiceCollection services) where TOperation : Operation
        {
            services.AddScoped<TOperation>();
        }
    }
}
