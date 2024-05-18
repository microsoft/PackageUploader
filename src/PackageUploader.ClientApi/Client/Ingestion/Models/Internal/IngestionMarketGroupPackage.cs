// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal class IngestionMarketGroupPackage
{
    /// <summary>
    /// Id of market group
    /// </summary>
    public string MarketGroupId { get; set; }
        
    /// <summary>
    /// Name of market group
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of markets
    /// </summary>
    public List<string> Markets { get; set; }

    /// <summary>
    /// List of packages
    /// </summary>
    public List<string> PackageIds { get; set; }

    /// <summary>
    /// Mandatory update
    /// </summary>
    public IngestionMandatoryUpdateInfo MandatoryUpdateInfo { get; set; }

    /// <summary>
    /// Schedule release date per region
    /// </summary>
    public DateTime? AvailabilityDate { get; set; }

    /// <summary>
    /// Dictionary of per region, per package scheduled release dates for XVC and MSIXVC packages
    /// </summary>
    public Dictionary<string, DateTime?> PackageAvailabilityDates { get; set; }

    /// <summary>
    /// Dictionary of per package metadata (e.g. predownload date) for XVC and MSIXVC packages
    /// </summary>
    public Dictionary<string, IngestionMarketGroupPackageMetadata> PackageIdToMetadataMap { get; set; }
}