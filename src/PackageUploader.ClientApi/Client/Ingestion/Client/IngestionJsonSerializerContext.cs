// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System.Text.Json.Serialization;

namespace PackageUploader.ClientApi.Client.Ingestion.Client;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(PagedCollection<IngestionGamePackage>))]
[JsonSerializable(typeof(PagedCollection<IngestionGameProduct>))]
[JsonSerializable(typeof(PagedCollection<IngestionBranch>))]
[JsonSerializable(typeof(PagedCollection<IngestionFlight>))]
[JsonSerializable(typeof(PagedCollection<IngestionGamePackageConfiguration>))]
[JsonSerializable(typeof(PagedCollection<IngestionSubmissionValidationItem>))]
[JsonSerializable(typeof(IngestionSubmission))]
[JsonSerializable(typeof(IngestionSubmissionCreationRequest))]
[JsonSerializable(typeof(IngestionPackageCreationRequest))]
[JsonSerializable(typeof(IngestionGamePackageAsset))]
[JsonSerializable(typeof(IngestionRedirectPackage))]
internal partial class IngestionJsonSerializerContext : JsonSerializerContext
{ }