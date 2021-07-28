// Copyright (C) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    public sealed class PagedCollection<T>
    {
        /// <summary>
        /// Gets the current page of elements.
        /// </summary>
        [JsonPropertyName("value")]
        public IList<T> Value { get; set; }
    }
}