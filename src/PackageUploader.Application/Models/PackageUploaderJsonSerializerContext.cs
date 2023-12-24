// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PackageUploader.Application.Models;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(IEnumerable<Package>))]
[JsonSerializable(typeof(Product))]
public partial class PackageUploaderJsonSerializerContext : JsonSerializerContext
{}