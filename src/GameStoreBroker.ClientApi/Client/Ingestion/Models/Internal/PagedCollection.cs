// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class PagedCollection<T>
    {
        /// <summary>
        /// Gets the current page of elements.
        /// </summary>
        public IList<T> Value { get; set; }

        /// <summary>
        /// Location URI for the next page (if applicable). If there is no next page, the property is not returned
        /// </summary>
        public string NextLink { get; set; }
    }
}