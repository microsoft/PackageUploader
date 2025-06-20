// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class AzureApplicationSecretAuthInfoValidator : IValidateOptions<AzureApplicationSecretAuthInfo>
{ }

public sealed class AzureApplicationSecretAuthInfo : AadAuthInfo
{
    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}