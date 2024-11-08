// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class AzurePipelinesAuthInfoValidator : IValidateOptions<AzurePipelinesAuthInfo>
{ }

public sealed class AzurePipelinesAuthInfo
{
    public const string ConfigName = nameof(AzurePipelinesAuthInfo);

    [Required]
    public string TenantId { get; set; } = Environment.GetEnvironmentVariable("AZURESUBSCRIPTION_TENANT_ID");

    [Required]
    public string ClientId { get; set; } = Environment.GetEnvironmentVariable("AZURESUBSCRIPTION_CLIENT_ID");

    [Required]
    public string ServiceConnectionId { get; set; } = Environment.GetEnvironmentVariable("AZURESUBSCRIPTION_SERVICE_CONNECTION_ID");

    [Required]
    public string SystemAccessToken { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");
}