// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;

[OptionsValidator]
public partial class AccessTokenProviderConfigValidator : IValidateOptions<AccessTokenProviderConfig>
{ }

public class AccessTokenProviderConfig
{
    [Required]
    public string AadAuthorityBaseUrl { get; set; } = "https://login.microsoftonline.com/";

    [Required]
    public string AadResourceForCaller { get; set; } = "https://api.partner.microsoft.com";

    [Required]
    public string AzureManagementBaseUrl { get; set; } = "https://management.azure.com/";
}