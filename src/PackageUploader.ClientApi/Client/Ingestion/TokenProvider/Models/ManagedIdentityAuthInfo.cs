// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class ManagedIdentityAuthInfoValidator : IValidateOptions<ManagedIdentityAuthInfo>
{ }

public sealed class ManagedIdentityAuthInfo
{
    public const string ConfigName = nameof(AadAuthInfo);

    [Required]
    public string ClientId { get; set; }
}