// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.ClientApi.Client.Xfus.Models
{
    public class XfusUploadInfo
    {
        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Xfus asset Id
        /// </summary>
        public Guid XfusId { get; set; }

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
