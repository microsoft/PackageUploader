// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class ManagedIdentityFederatedAuthInfoValidator : IValidateOptions<ManagedIdentityFederatedAuthInfo>
{ }

public sealed class ManagedIdentityFederatedAuthInfo
{
    public const string ConfigName = nameof(AadAuthInfo);

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string ApplicationTenantId { get; set; }

    [Required]
    public string ApplicationClientId { get; set; }
}