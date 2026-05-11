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
    /// <summary>
    /// Per-request timeout in milliseconds for non-upload XFUS HTTP calls
    /// (e.g., initialize, continue, status checks).
    /// Must be high enough to accommodate XVC initialize, which involves
    /// multiple sequential storage calls and can take 2-6s cross-region.
    /// A value that is too low (e.g., 5000ms) will cause intermittent
    /// <see cref="System.Threading.Tasks.TaskCanceledException"/> failures
    /// for clients in distant regions. Default is 300000ms (5 minutes).
    /// The README example recommends 60000ms (60s) as a reasonable override.
    /// </summary>
    [Required]
    public int HttpTimeoutMs { get; set; } = 300000;

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
