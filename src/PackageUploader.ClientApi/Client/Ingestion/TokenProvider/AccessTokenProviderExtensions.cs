// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

internal static class AccessTokenProviderExtensions
{
    public static IServiceCollection AddAzureApplicationSecretAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AzureApplicationSecretAuthInfo>, AzureApplicationSecretAuthInfoValidator>();
        services.AddOptions<AzureApplicationSecretAuthInfo>().BindConfiguration(AadAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzureApplicationSecretAccessTokenProvider>();

        return services;
    }

    public static IServiceCollection AddAzureApplicationCertificateAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AzureApplicationCertificateAuthInfo>, AzureApplicationCertificateAuthInfoValidator>();
        services.AddOptions<AzureApplicationCertificateAuthInfo>().BindConfiguration(AadAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzureApplicationCertificateAccessTokenProvider>();

        return services;
    }

    public static IServiceCollection AddAzureCliCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzureCliCredentialAccessTokenProvider>();

        return services;
    }

    public static IServiceCollection AddDefaultAzureCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, DefaultAzureCredentialAccessTokenProvider>();

        return services;
    }

    public static IServiceCollection AddInteractiveBrowserCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, InteractiveBrowserCredentialAccessTokenProvider>();

        return services;
    }

    private static void AddAccessTokenProviderOptions(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AccessTokenProviderConfig>, AccessTokenProviderConfigValidator>();
        services.AddOptions<AccessTokenProviderConfig>().BindConfiguration(nameof(AccessTokenProviderConfig));
    }
}