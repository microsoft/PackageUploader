// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using PackageUploader.ClientApi;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Command line values that are not part of the bound operation configuration but are still needed
/// to build MakePkg.exe arguments.
/// </summary>
internal sealed record Msixvc2CommandLineContext(IngestionExtensions.AuthenticationMethod AuthenticationMethod, string? TenantId);
