// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public class GamePackageResource
    {
        /// <summary>
        /// ETag
        /// </summary>
        public string ODataETag { get; internal init; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; internal init; }
    }
}
