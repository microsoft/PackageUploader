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
    }

    public static IServiceCollection AddPackageUploaderService(this IServiceCollection services,
        AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret) =>
        services
            .AddScoped<IPackageUploaderService, PackageUploaderService>()
            .AddIngestionService()
            .AddIngestionAuthentication(authenticationMethod)
            .AddXfusService();

    private static IServiceCollection AddIngestionAuthentication(this IServiceCollection services, 
        AuthenticationMethod authenticationMethod = AuthenticationMethod.AppSecret) =>
        authenticationMethod switch
        {
            AuthenticationMethod.AppSecret => services.AddAzureApplicationSecretAccessTokenProvider(),
            AuthenticationMethod.AppCert => services.AddAzureApplicationCertificateAccessTokenProvider(),
            AuthenticationMethod.Browser => services.AddInteractiveBrowserCredentialAccessTokenProvider(),
            AuthenticationMethod.Default => services.AddDefaultAzureCredentialAccessTokenProvider(),
            AuthenticationMethod.AzureCli => services.AddAzureCliCredentialAccessTokenProvider(),
            _ => services.AddAzureApplicationSecretAccessTokenProvider(),
        };
}