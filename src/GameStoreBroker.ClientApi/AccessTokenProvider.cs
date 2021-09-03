// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly AadAuthInfo _aadAuthInfo;
        private const string AadAuthorityBaseUrl = "https://login.microsoftonline.com/";
        private const string AadResourceForCaller = "https://api.partner.microsoft.com";

        public AccessTokenProvider(IOptions<AadAuthInfo> aadAuthInfo)
        {
            _aadAuthInfo = aadAuthInfo?.Value ?? throw new ArgumentNullException(nameof(aadAuthInfo), $"{nameof(aadAuthInfo)} cannot be null.");

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.TenantId))
            {
                throw new ArgumentException("TenantId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientId))
            {
                throw new ArgumentException("ClientId not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }

            if (string.IsNullOrWhiteSpace(_aadAuthInfo.ClientSecret))
            {
                throw new ArgumentException("ClientSecret not provided in AadAuthInfo.", nameof(aadAuthInfo));
            }
        }

        public async Task<string> GetAccessToken()
        {
            var authority = AadAuthorityBaseUrl + _aadAuthInfo.TenantId;
            var authenticationContext = new AuthenticationContext(authority, true);

            var clientCredential = new ClientCredential(_aadAuthInfo.ClientId, _aadAuthInfo.ClientSecret);
            var result = await authenticationContext.AcquireTokenAsync(AadResourceForCaller, clientCredential).ConfigureAwait(false);

            if (result is null)
            {
                throw new Exception("Failure while acquiring token.");
            }

            return result.AccessToken;
        }
    }
}