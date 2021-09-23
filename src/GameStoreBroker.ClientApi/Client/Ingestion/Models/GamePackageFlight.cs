// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GamePackageFlight : GamePackageResource
    {
        /// <summary>
        /// Branch friendly Name
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Flight name
        /// </summary>
        public string FlightName { get; internal init; }

        /// <summary>
        /// Flight group ids
        /// </summary>
        public IList<string> GroupIds { get; set; }

        /// <summary>
        /// Branch current draft instance ID.
        /// </summary>
        public string CurrentDraftInstanceId { get; internal init; }
    }
}
