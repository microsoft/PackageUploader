// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    /// <summary>
    /// Service to coordinate authentication token resets across components when authentication state changes.
    /// </summary>
    public interface IAuthenticationResetService
    {
        /// <summary>
        /// Resets all authentication tokens and caches, ensuring the next authentication request uses fresh credentials.
        /// </summary>
        void ResetAuthentication();
    }
}