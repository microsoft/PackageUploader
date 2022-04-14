// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal;

internal class IngestionGamePackageConfiguration : IngestionResource
{
    /// <summary>
    /// Resource type [GamePackageConfiguration]
    /// </summary>
    public string ResourceType { get; set; }
        
    /// <summary>
    /// Display name of the branch this PackageSet belongs to
    /// </summary>
    public string BranchName { get; set; }

    /// <summary>
    /// ID of the branch this PackageSet belongs to
    /// </summary>
    public string BranchId { get; set; }

    /// <summary>
    /// The BranchType for the branch this PackageSet belongs to.
    /// </summary>
    public string BranchType { get; set; }

    /// <summary>
    /// Created date time
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// ModifiedDate date time
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Gradual Rollout information
    /// </summary>
    public IngestionGradualRolloutInfo GradualRolloutInfo { get; set; }

    /// <summary>
    /// List of market groups
    /// </summary>
    public List<IngestionMarketGroupPackage> MarketGroupPackages { get; set; }
}