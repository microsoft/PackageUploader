// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Client;

/// <summary>
/// Configuration for the UploadSource HTTP header sent with every Partner Center Ingestion API request.
/// This header identifies which tool originated the request, enabling server-side telemetry and diagnostics.
/// The header value is validated against a case-insensitive allowlist. Any value not in the allowlist
/// silently falls back to "PackageUploader". This prevents arbitrary or malicious values from being sent on the wire.
/// Injected into IngestionHttpClient → HttpRestClient, which adds the header in CreateJsonRequestMessage().
/// </summary>
internal class UploadSourceConfig
{
    /// Default header value used by the Package Uploader CLI.
    public const string PackageUploaderSource = "PackageUploader";

    /// Case-insensitive set of permitted UploadSource values.
    private static readonly HashSet<string> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
    {
        PackageUploaderSource,
    };

    /// The UploadSource value to send. Defaults to PackageUploaderSource.
    public string UploadSource { get; init; } = PackageUploaderSource;

    /// Returns true if value is a non-empty, allowlisted upload source.
    public static bool IsAllowedValue(string value) =>
        !string.IsNullOrWhiteSpace(value) && AllowedValues.Contains(value.Trim());
}
