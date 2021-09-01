// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly AadAuthInfo _aadAuthInfo;
        private const string AadAuthorityBaseUrl = "https://login.microsoftonline.com/";
        private const string AadResourceForCaller = "https://api.partner.microsoft.com";

        public AccessTokenProvider(AadAuthInfo aadAuthInfo)
        {
            if (aadAuthInfo == null)
            {
                throw new ArgumentNullException(nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(aadAuthInfo.TenantId))
            {
                throw new ArgumentException("TenantId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(aadAuthInfo.ClientId))
            {
                throw new ArgumentException("ClientId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(aadAuthInfo.ClientSecret))
            {
                throw new ArgumentException("ClientSecret not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            _aadAuthInfo = aadAuthInfo;
        }

        public async Task<string> GetAccessToken(CancellationToken ct)
        {
            var authority = AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
            var authenticationContext = new AuthenticationContext(authority, true);

            var clientCredential = new ClientCredential(_aadAuthInfo.ClientId, _aadAuthInfo.ClientSecret);
            var result = await authenticationContext.AcquireTokenAsync(AadResourceForCaller, clientCredential).ConfigureAwait(false);

            if (result == null)
            {
                throw new Exception("Failure while acquiring token.");
            }

            return result.AccessToken;
        }
    }
}