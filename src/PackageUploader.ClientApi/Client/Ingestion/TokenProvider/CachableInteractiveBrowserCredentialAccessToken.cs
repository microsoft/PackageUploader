using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider
{
    public class CachableInteractiveBrowserCredentialAccessToken : CredentialAccessTokenProvider, IAccessTokenProvider //InteractiveBrowserCredentialAccessTokenProvider
    {
        // See: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        // See: https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/blob/main/sample/ManualTestApp/Config.cs
        private static string TokenCacheName = ".PackageUploader_Cache.bin";
        private static string TokenCacheDir = MsalCacheHelper.UserRootDirectory;

        public CachableInteractiveBrowserCredentialAccessToken(IOptions<AccessTokenProviderConfig> config, ILogger<CachableInteractiveBrowserCredentialAccessToken> logger) : base(config, logger)
        { }

        public async Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct)
        {
            // See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/TokenCache.md
            AuthenticationRecord record = null;
            string recordPath = Path.Combine(TokenCacheDir, TokenCacheName);
            if (File.Exists(recordPath)) // With thanks to the Greenbelt team
            {
                using (var stream = new FileStream(recordPath, FileMode.Open, FileAccess.Read))
                {
                    record = AuthenticationRecord.Deserialize(stream);
                }
            }
            var azureCredentialOptions = SetTokenCredentialOptions(new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
                AuthenticationRecord = record
            });
            var azureCredential = new InteractiveBrowserCredential(azureCredentialOptions);

            // If we don't have a record, then we need to call AuthenticateAsync to get one.
            // That will ALWAYS bring up the browswer. If we HAVE a record, then we skip
            // directly to an azureCredential.GetTokenAsync() call which will try silently first.
            if (record is null) 
            { 
                record = await azureCredential.AuthenticateAsync();
                using (var stream = new FileStream(recordPath, FileMode.Create, FileAccess.Write))
                {
                    await record.SerializeAsync(stream);
                }
            }

            return await GetIngestionAccessTokenAsync(azureCredential, ct).ConfigureAwait(false);
        }
    }
}
