// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Xfus;
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
        ManagedIdentity,
        Environment,
        AzurePipelines,
        ClientSecret,
        ClientCertificate,
        CacheableBrowser,
    }

    public static IServiceCollection AddPackageUploaderService(this IServiceCollection services,
        AuthenticationMethod authenticationMethod = AuthenticationMethod.Default) =>
        services
            .AddScoped<IPackageUploaderService, PackageUploaderService>()
            .AddIngestionService()
            .AddIngestionAuthentication(authenticationMethod)
            .AddXfusService();

    private static IServiceCollection AddIngestionAuthentication(this IServiceCollection services, 
        AuthenticationMethod authenticationMethod = AuthenticationMethod.Default) =>
        authenticationMethod switch
        {
            AuthenticationMethod.AppSecret => services.AddAzureApplicationSecretAccessTokenProvider(),
            AuthenticationMethod.AppCert => services.AddAzureApplicationCertificateAccessTokenProvider(),
            AuthenticationMethod.Browser => services.AddInteractiveBrowserCredentialAccessTokenProvider(),
            AuthenticationMethod.Default => services.AddDefaultAzureCredentialAccessTokenProvider(),
            AuthenticationMethod.AzureCli => services.AddAzureCliCredentialAccessTokenProvider(),
            AuthenticationMethod.ManagedIdentity => services.AddManagedIdentityCredentialAccessTokenProvider(),
            AuthenticationMethod.Environment => services.AddEnvironmentCredentialAccessTokenProvider(),
            AuthenticationMethod.AzurePipelines => services.AddAzurePipelinesCredentialAccessTokenProvider(),
            AuthenticationMethod.ClientSecret => services.AddClientSecretCredentialAccessTokenProvider(),
            AuthenticationMethod.ClientCertificate => services.AddClientCertificateCredentialAccessTokenProvider(),
            AuthenticationMethod.CacheableBrowser => services.AddCacheableInteractiveBrowserCredentialAccessTokenProvider(),
            _ => services.AddAzureApplicationSecretAccessTokenProvider(),
        };
}