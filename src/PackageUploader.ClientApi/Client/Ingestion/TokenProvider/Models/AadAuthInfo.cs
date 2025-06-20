// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

public abstract class AadAuthInfo
{
    public const string ConfigName = nameof(AadAuthInfo);

    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;
}