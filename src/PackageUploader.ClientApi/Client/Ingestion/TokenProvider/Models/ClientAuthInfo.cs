// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

public class ClientSecretAuthInfo
{
    public const string ConfigName = nameof(ClientSecretAuthInfo);

    [Required]
    public string TenantId { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; }
}

public class ClientCertificateAuthInfo
{
    public const string ConfigName = nameof(ClientCertificateAuthInfo);

    [Required]
    public string TenantId { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string CertificatePath { get; set; }

    public string CertificatePassword { get; set; }
}

public class ClientSecretAuthInfoValidator : IValidateOptions<ClientSecretAuthInfo>
{
    public ValidateOptionsResult Validate(string name, ClientSecretAuthInfo options)
    {
        if (string.IsNullOrEmpty(options.TenantId))
        {
            return ValidateOptionsResult.Fail("TenantId is required");
        }

        if (string.IsNullOrEmpty(options.ClientId))
        {
            return ValidateOptionsResult.Fail("ClientId is required");
        }

        if (string.IsNullOrEmpty(options.ClientSecret))
        {
            return ValidateOptionsResult.Fail("ClientSecret is required");
        }

        return ValidateOptionsResult.Success;
    }
}

public class ClientCertificateAuthInfoValidator : IValidateOptions<ClientCertificateAuthInfo>
{
    public ValidateOptionsResult Validate(string name, ClientCertificateAuthInfo options)
    {
        if (string.IsNullOrEmpty(options.TenantId))
        {
            return ValidateOptionsResult.Fail("TenantId is required");
        }

        if (string.IsNullOrEmpty(options.ClientId))
        {
            return ValidateOptionsResult.Fail("ClientId is required");
        }

        if (string.IsNullOrEmpty(options.CertificatePath))
        {
            return ValidateOptionsResult.Fail("CertificatePath is required");
        }

        return ValidateOptionsResult.Success;
    }
}