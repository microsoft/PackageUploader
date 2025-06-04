// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    /// <summary>
    /// Provides a way to reset authentication tokens and caches across the application
    /// </summary>
    public class AuthenticationResetService : IAuthenticationResetService
    {
        private readonly IEnumerable<IngestionAuthenticationDelegatingHandler> _authHandlers;
        private readonly ILogger<AuthenticationResetService> _logger;

        public AuthenticationResetService(
            IEnumerable<IngestionAuthenticationDelegatingHandler> authHandlers,
            ILogger<AuthenticationResetService> logger)
        {
            _authHandlers = authHandlers;
            _logger = logger;
        }

        /// <summary>
        /// Resets all authentication tokens and caches
        /// </summary>
        public void ResetAuthentication()
        {
            try
            {
                _logger.LogInformation("Resetting authentication state");
                
                // Clear the MSAL token cache file
                CachableInteractiveBrowserCredentialAccessToken.ClearCache();
                
                // Reset any active tokens in HTTP handlers
                foreach (var handler in _authHandlers)
                {
                    handler.ResetToken();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting authentication state");
            }
        }
    }
}