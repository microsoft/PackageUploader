// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

namespace PackageUploader.ClientApi.Client.Ingestion.Client;

[JsonSerializable(typeof(PagedCollection<IngestionGamePackage>))]
[JsonSerializable(typeof(PagedCollection<IngestionGameProduct>))]
[JsonSerializable(typeof(PagedCollection<IngestionBranch>))]
[JsonSerializable(typeof(PagedCollection<IngestionFlight>))]
[JsonSerializable(typeof(PagedCollection<IngestionGamePackageConfiguration>))]
[JsonSerializable(typeof(PagedCollection<IngestionSubmissionValidationItem>))]
[JsonSerializable(typeof(IngestionGamePackage))]
[JsonSerializable(typeof(IngestionGameProduct))]
[JsonSerializable(typeof(IngestionBranch))]
[JsonSerializable(typeof(IngestionFlight))]
[JsonSerializable(typeof(IngestionGamePackageConfiguration))]
[JsonSerializable(typeof(IngestionSubmissionValidationItem))]
[JsonSerializable(typeof(IngestionSubmission))]
[JsonSerializable(typeof(IngestionSubmissionCreationRequest))]
[JsonSerializable(typeof(IngestionPackageCreationRequest))]
[JsonSerializable(typeof(IngestionGamePackageAsset))]
internal partial class IngestionJsonSerializerContext : JsonSerializerContext
{ }