// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal sealed class IngestionGameProduct
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public bool? IsModularPublishing { get; set; }
        
        public IList<TypeValuePair> ExternalIds { get; set; }
    }
}