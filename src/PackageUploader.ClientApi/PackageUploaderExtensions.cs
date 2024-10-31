// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Xfus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PackageUploader.ClientApi;

public static class IngestionExtensions
{
    public enum AuthenticationMethod 
    {
        AppSecret,
        AppCert,
        Default, 
        Browser,
        AzureCli,
    }

    public static IServiceCollection AddPackageUploaderService(this IServiceCollection services, IConfiguration config, 
        AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret)
    {
        services.AddScoped<IPackageUploaderService, PackageUploaderService>();
        services.AddIngestionService(config);
        services.AddIngestionAuthentication(config, authenticationMethod);
        services.AddXfusService(config);

        return services;
    }

    private static IServiceCollection AddIngestionAuthentication(this IServiceCollection services, IConfiguration config, 
        AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret) =>
        authenticationMethod switch
        {
            AuthenticationMethod.AppSecret => services.AddAzureApplicationSecretAccessTokenProvider(config),
            AuthenticationMethod.AppCert => services.AddAzureApplicationCertificateAccessTokenProvider(config),
            AuthenticationMethod.Browser => services.AddInteractiveBrowserCredentialAccessTokenProvider(config),
            AuthenticationMethod.Default => services.AddDefaultAzureCredentialAccessTokenProvider(config),
            AuthenticationMethod.AzureCli => services.AddAzureCliAccessTokenProvider(config),
            _ => services.AddAzureApplicationSecretAccessTokenProvider(config),
        };
}