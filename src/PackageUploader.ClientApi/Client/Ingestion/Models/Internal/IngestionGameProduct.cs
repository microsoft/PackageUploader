// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal sealed class IngestionGameProduct
{
    /// <summary>
    /// Resource type
    /// </summary>
    public string ResourceType { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Collection of external Id
    /// </summary>
    public IList<TypeValuePair> ExternalIds { get; set; }

    /// <summary>
    /// Flag of if product is modular-publishing or not
    /// </summary>
    public bool? IsModularPublishing { get; set; }

    /// <summary>
    /// Resource ID
    /// </summary>
    public string Id { get; set; }
}