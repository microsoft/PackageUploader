// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="GameProduct.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace GameStoreBroker.Api.ExternalModels
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// GameProduct Model
    /// </summary>
    public sealed class GamePackageBranch
    {
        /// <summary>
        /// LongId
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Collection of external Id
        /// Type: AzurePublisherId, AzureOfferId, ExternalAzureProductId
        /// </summary>
        public string Name { get; set; }

        public string CurrentDraftInstanceID { get; set; }
    }
}