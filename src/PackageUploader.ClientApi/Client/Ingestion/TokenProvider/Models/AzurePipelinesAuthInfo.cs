// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

[OptionsValidator]
internal partial class AzurePipelinesAuthInfoValidator : IValidateOptions<AzurePipelinesAuthInfo>
{ }

public sealed class AzurePipelinesAuthInfo : AadAuthInfo
{
    [Required]
    public string ServiceConnectionId { get; set; }
}