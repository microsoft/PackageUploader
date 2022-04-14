// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

public sealed class AzureApplicationSecretAuthInfo : AadAuthInfo
{
    [Required]
    public string ClientSecret { get; set; }
}