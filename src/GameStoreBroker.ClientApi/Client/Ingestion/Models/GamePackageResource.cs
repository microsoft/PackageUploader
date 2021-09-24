// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public abstract class GamePackageResource
    {
        /// <summary>
        /// ETag
        /// </summary>
        internal string ETag { get; init; }

        /// <summary>
        /// ETag
        /// </summary>
        internal string ODataETag { get; init; }

        /// <summary>
        /// Resource ID
        /// </summary>
        public string Id { get; internal init; }
    }
}
