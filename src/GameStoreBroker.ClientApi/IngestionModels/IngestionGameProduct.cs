// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="GameProduct.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace GameStoreBroker.ClientApi.IngestionModels
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    internal sealed class IngestionGameProduct
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; internal set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "isModularPublishing")]
        public bool? IsModularPublishing { get; set; }

        [JsonProperty(PropertyName = "externalIDs")]
        public IList<TypeValuePair> ExternalIds { get; set; }
    }
}