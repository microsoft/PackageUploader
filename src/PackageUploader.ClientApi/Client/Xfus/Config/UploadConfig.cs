// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.ClientApi.Client.Xfus.Config;

[OptionsValidator]
public partial class UploadConfigValidator : IValidateOptions<UploadConfig>
{ }

public class UploadConfig
{
    [Required]
    public int HttpTimeoutMs { get; set; } = 10000;

    [Required]
    public int HttpUploadTimeoutMs { get; set; } = 300000;

    [Required]
    public int MaxParallelism { get; set; } = 24;

    [Required]
    public int DefaultConnectionLimit { get; set; } = -1;

    [Required]
    public bool Expect100Continue { get; set; } = false;

    [Required]
    public bool UseNagleAlgorithm { get; set; } = false;

    public int RetryCount { get; set; } = 3;
}
