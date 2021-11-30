// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.Models
{
    public sealed class GameProduct
    {
        /// <summary>
        /// Product Id, aka. LongId - looks like a long number
        /// </summary>
        public string ProductId { get; internal init; }

        /// <summary>
        /// Big Id, combination of letters/numbers usually beginning with 9
        /// </summary>
        public string BigId { get; internal init; }

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; internal init; }

        /// <summary>
        /// Flag of if product is modular-publishing (Jaguar) or not
        /// </summary>
        public bool IsJaguar { get; internal init; }
    }
}
