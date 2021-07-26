// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="GameProduct.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace GameStoreBroker.ClientApi.Models
{
    /// <summary>
    /// GameProduct Model
    /// </summary>
    public sealed class GameProduct
    {
        /// <summary>
        /// Product Id, aka. LongId - looks like a long number
        /// </summary>
        public string ProductId { get; internal set; }

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