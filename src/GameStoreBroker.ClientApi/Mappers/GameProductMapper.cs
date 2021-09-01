// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion.Models;
using GameStoreBroker.ClientApi.Models;
using System;
using System.Linq;

namespace GameStoreBroker.ClientApi.Mappers
{
    internal static class GameProductMapper
    {
        public static GameProduct Map(this IngestionGameProduct ingestionGameProduct)
        {
            return new GameProduct
            {
                ProductId = ingestionGameProduct.Id,
                BigId = ingestionGameProduct.ExternalIds?.FirstOrDefault(id => id.Type.Equals("StoreId", StringComparison.OrdinalIgnoreCase))?.Value,
                ProductName = ingestionGameProduct.Name,
                IsJaguar = ingestionGameProduct.IsModularPublishing.HasValue && ingestionGameProduct.IsModularPublishing.Value
            };
        }
    }
}
