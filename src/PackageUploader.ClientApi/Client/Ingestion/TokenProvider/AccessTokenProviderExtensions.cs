// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

internal static class AccessTokenProviderExtensions
{
    public static IServiceCollection AddAzureApplicationSecretAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
        services.AddAccessTokenProvider<AzureApplicationSecretAccessTokenProvider, AzureApplicationSecretAuthInfo>(config);

    public static IServiceCollection AddAzureApplicationCertificateAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
        services.AddAccessTokenProvider<AzureApplicationCertificateAccessTokenProvider, AzureApplicationCertificateAuthInfo>(config);

    public static IServiceCollection AddDefaultAzureCredentialAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
        services.AddAccessTokenProvider<DefaultAzureCredentialAccessTokenProvider>(config);

    public static IServiceCollection AddInteractiveBrowserCredentialAccessTokenProvider(this IServiceCollection services, IConfiguration config) =>
        services.AddAccessTokenProvider<InteractiveBrowserCredentialAccessTokenProvider>(config);

    private static IServiceCollection AddAccessTokenProvider<TProvider, TAuthInfo>(this IServiceCollection services, IConfiguration config)
        where TProvider : class, IAccessTokenProvider where TAuthInfo : AadAuthInfo
    {
        services.AddOptions<TAuthInfo>().Bind(config.GetSection(AadAuthInfo.ConfigName)).ValidateDataAnnotations();
        services.AddAccessTokenProvider<TProvider>(config);
        return services;
    }

    private static IServiceCollection AddAccessTokenProvider<TProvider>(this IServiceCollection services, IConfiguration config)
        where TProvider : class, IAccessTokenProvider
    {
        services.AddOptions<AccessTokenProviderConfig>().Bind(config.GetSection(nameof(AccessTokenProviderConfig))).ValidateDataAnnotations();
        services.AddScoped<IAccessTokenProvider, TProvider>();
        return services;
    }
}