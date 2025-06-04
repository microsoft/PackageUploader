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
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddAzureApplicationCertificateAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AzureApplicationCertificateAuthInfo>, AzureApplicationCertificateAuthInfoValidator>();
        services.AddOptions<AzureApplicationCertificateAuthInfo>().BindConfiguration(AadAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzureApplicationCertificateAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddAzureCliCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzureCliCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddManagedIdentityCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<ManagedIdentityAuthInfo>, ManagedIdentityAuthInfoValidator>();
        services.AddOptions<ManagedIdentityAuthInfo>().BindConfiguration(ManagedIdentityAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, ManagedIdentityCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddDefaultAzureCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, DefaultAzureCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddInteractiveBrowserCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddOptions<BrowserAuthInfo>().BindConfiguration(BrowserAuthInfo.ConfigName);
        services.AddScoped<IAccessTokenProvider, InteractiveBrowserCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddCacheableInteractiveBrowserCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddOptions<BrowserAuthInfo>().BindConfiguration(BrowserAuthInfo.ConfigName);
        services.AddAzureTenantServices();
        services.AddScoped<IAccessTokenProvider, CachableInteractiveBrowserCredentialAccessToken>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddEnvironmentCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, EnvironmentCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddAzurePipelinesCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AzurePipelinesAuthInfo>, AzurePipelinesAuthInfoValidator>();
        services.AddOptions<AzurePipelinesAuthInfo>().BindConfiguration(AzurePipelinesAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, AzurePipelinesCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddClientSecretCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<ClientSecretAuthInfo>, ClientSecretAuthInfoValidator>();
        services.AddOptions<ClientSecretAuthInfo>().BindConfiguration(ClientSecretAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, ClientSecretCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddClientCertificateCredentialAccessTokenProvider(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<ClientCertificateAuthInfo>, ClientCertificateAuthInfoValidator>();
        services.AddOptions<ClientCertificateAuthInfo>().BindConfiguration(ClientCertificateAuthInfo.ConfigName);

        services.AddAccessTokenProviderOptions();
        services.AddScoped<IAccessTokenProvider, ClientCertificateCredentialAccessTokenProvider>();
        services.AddAuthenticationResetService();

        return services;
    }

    public static IServiceCollection AddAzureTenantServices(this IServiceCollection services)
    {
        services.AddHttpClient<IAzureTenantService, AzureTenantService>();
        services.AddScoped<IAzureTenantService, AzureTenantService>();
        return services;
    }

    public static IServiceCollection AddAuthenticationResetService(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationResetService, AuthenticationResetService>();
        return services;
    }

    private static void AddAccessTokenProviderOptions(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AccessTokenProviderConfig>, AccessTokenProviderConfigValidator>();
        services.AddOptions<AccessTokenProviderConfig>().BindConfiguration(nameof(AccessTokenProviderConfig));
    }
}