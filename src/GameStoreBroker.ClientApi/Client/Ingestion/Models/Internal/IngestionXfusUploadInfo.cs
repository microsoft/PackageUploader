// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionXfusUploadInfo
    {
        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Xfus asset Id
        /// </summary>
        public string XfusId { get; set; }

        /// <summary>
        /// file SAS URI
        /// </summary>
        public string FileSasUri { get; set; }

        /// <summary>
        /// Xfus token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Xfus upload domain
        /// </summary>
        public string UploadDomain { get; set; }

        /// <summary>
        /// Xfus tenant, e.g. DCE, XICE
        /// </summary>
        public string XfusTenant { get; set; }
    }
}
