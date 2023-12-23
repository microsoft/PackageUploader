// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Xfus.Models.Internal;
using System.Text.Json.Serialization;

namespace PackageUploader.ClientApi.Client.Xfus.Uploader;

[JsonSerializable(typeof(UploadProperties))]
internal partial class XfusJsonSerializerContext : JsonSerializerContext
{ }