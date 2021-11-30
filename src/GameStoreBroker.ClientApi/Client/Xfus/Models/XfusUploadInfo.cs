// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Xfus.Models
{
    public class XfusUploadInfo
    {
        /// <summary>
        /// Xfus asset Id
        /// </summary>
        public Guid XfusId { get; set; }

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
