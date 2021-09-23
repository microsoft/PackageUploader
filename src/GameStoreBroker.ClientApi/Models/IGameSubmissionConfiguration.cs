// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.ClientApi.Models
{
    public interface IGameSubmissionConfiguration
    {
        /// <summary>
        /// Certification notes
        /// </summary>
        public string CertificationNotes { get; set; }
    }
}