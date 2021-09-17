// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models
{
    /// <summary>
    /// GameProduct Model
    /// </summary>
    public sealed class GameProduct
    {
        /// <summary>
        /// Product Id, aka. LongId - looks like a long number
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Big Id, combination of letters/numbers usually begining with 9
        /// </summary>
        public string BigId { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Flag of if product is modular-pubishing (Jaguar) or not
        /// </summary>
        public bool IsJaguar { get; set; }
    }
}