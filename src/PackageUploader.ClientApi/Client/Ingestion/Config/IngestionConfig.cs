// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace PackageUploader.ClientApi.Client.Ingestion.Config;

[OptionsValidator]
public partial class IngestionConfigValidator : IValidateOptions<IngestionConfig>
{ }

public class IngestionConfig
{
    [Required]
    public string BaseAddress { get; set; } = "https://api.partner.microsoft.com/v1.0/ingestion/";

    [Required]
    public int HttpTimeoutMs { get; set; } = 600000;

    [Required]
    public int RetryCount { get; set; } = 5;

    [Required]
    public int MedianFirstRetryDelayMs { get; set; } = 1000;
}