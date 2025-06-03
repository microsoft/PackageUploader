using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Extensions.Msal;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    public class CachableInteractiveBrowserCredentialAccessToken : CredentialAccessTokenProvider, IAccessTokenProvider
    {
        // See: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        // See: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/main/sample/ManualTestApp/Config.cs
        private static readonly string TokenCacheName = ".PackageUploader_Cache.bin";
        private static readonly string TokenCacheDir = MsalCacheHelper.UserRootDirectory;
        private readonly IAzureTenantService _tenantService;
        private string _tenantId;

        public CachableInteractiveBrowserCredentialAccessToken(
            IOptions<AccessTokenProviderConfig> config, 
            ILogger<CachableInteractiveBrowserCredentialAccessToken> logger,
            IAzureTenantService tenantService = null) : base(config, logger)
        {
            _tenantService = tenantService;
        }

        public static void ClearCache()
        {
            string recordPath = Path.Combine(TokenCacheDir, TokenCacheName);

            // Delete existing record if it exists
            if (File.Exists(recordPath))
            {
                File.Delete(recordPath);
            }
        }

        /// <summary>
        /// Gets available Azure tenants using the provided token credential
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of Azure tenants</returns>
        public async Task<AzureTenantList> GetTenantsAsync(CancellationToken ct = default)
        {
            if (_tenantService == null)
            {
                throw new InvalidOperationException("Tenant service is not initialized");
            }

            // Create a temporary credential for listing tenants
            var options = SetTokenCredentialOptions(new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
                AdditionallyAllowedTenants = { "*" }
            });
            var credential = new InteractiveBrowserCredential(options);

            return await _tenantService.GetTenantsAsync(credential, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the tenant ID to use for authentication
        /// </summary>
        /// <param name="tenantId">The tenant ID to use</param>
        public void SetTenantId(string tenantId)
        {
            _tenantId = tenantId;
        }

        /// <summary>
        /// Gets the currently set tenant ID
        /// </summary>
        public string GetTenantId() => _tenantId;

        public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
        {
            // See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/TokenCache.md
            AuthenticationRecord record = null;
            string recordPath = Path.Combine(TokenCacheDir, TokenCacheName);
            if (File.Exists(recordPath)) 
            {
                using (var stream = new FileStream(recordPath, FileMode.Open, FileAccess.Read))
                {
                    record = AuthenticationRecord.Deserialize(stream, ct);
                }
            }

            var azureCredentialOptions = SetTokenCredentialOptions(new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
                AuthenticationRecord = record,
                TenantId = _tenantId, // Use the specified tenant ID if available
                AdditionallyAllowedTenants = { "*" }
            });
            
            var azureCredential = new InteractiveBrowserCredential(azureCredentialOptions);

            // If we don't have a record, then we need to call AuthenticateAsync to get one.
            // That will ALWAYS bring up the browser. If we HAVE a record, then we skip
            // directly to an azureCredential.GetTokenAsync() call which will try silently first.
            if (record is null) 
            { 
                record = await azureCredential.AuthenticateAsync(ct);
                using (var stream = new FileStream(recordPath, FileMode.Create, FileAccess.Write))
                {
                    await record.SerializeAsync(stream, ct);
                }
            }

            return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
        }
    }
}
