﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.ClientApi.Client.Ingestion.Models.Internal
{
    internal class IngestionMandatoryUpdateInfo
    {
        /// <summary>
        /// Is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Mandatory version
        /// </summary>
        public string MandatoryVersion { get; set; }

        /// <summary>
        /// Effective date
        /// </summary>
        public DateTime? EffectiveDate { get; set; }
    }
}