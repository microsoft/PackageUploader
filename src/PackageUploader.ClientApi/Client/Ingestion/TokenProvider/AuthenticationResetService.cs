// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    /// <summary>
    /// Static helper for auth token management - needed because HTTP message handlers can be created in 
    /// different DI scopes that the reset service can't access directly
    /// </summary>
    public static class AuthenticationTokenManager
    {
        private static readonly List<WeakReference<IngestionAuthenticationDelegatingHandler>> _handlers
            = new List<WeakReference<IngestionAuthenticationDelegatingHandler>>();

        /// <summary>
        /// Register a handler to be notified when tokens should be reset
        /// </summary>
        public static void RegisterHandler(IngestionAuthenticationDelegatingHandler handler)
        {
            lock (_handlers)
            {
                // Clean up any dead references first
                _handlers.RemoveAll(h => !h.TryGetTarget(out _));
                _handlers.Add(new WeakReference<IngestionAuthenticationDelegatingHandler>(handler));
            }
        }

        /// <summary>
        /// Reset all active token handlers
        /// </summary>
        public static void ResetAllTokens()
        {
            lock (_handlers)
            {
                // Find all handlers that are still alive and reset them
                foreach (var weakRef in _handlers.ToArray())
                {
                    if (weakRef.TryGetTarget(out var handler))
                    {
                        handler.ResetToken();
                    }
                }

                // Clean up dead references
                _handlers.RemoveAll(h => !h.TryGetTarget(out _));
            }
        }
    }

    /// <summary>
    /// Provides a way to reset authentication tokens and caches across the application
    /// </summary>
    public class AuthenticationResetService : IAuthenticationResetService
    {
        private readonly ILogger<AuthenticationResetService> _logger;

        public AuthenticationResetService(ILogger<AuthenticationResetService> logger)
        {
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
                
                // Reset all registered authentication handlers
                AuthenticationTokenManager.ResetAllTokens();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting authentication state");
            }
        }
    }
}