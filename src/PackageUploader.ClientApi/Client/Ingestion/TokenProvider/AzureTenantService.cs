// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Client.Ingestion.TokenProvider;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AzureTenantList))]
[JsonSerializable(typeof(AzureTenant))]
internal partial class AzureTenantJsonContext : JsonSerializerContext { }

public interface IAzureTenantService
{
    Task<AzureTenantList> GetTenantsAsync(TokenCredential tokenCredential, CancellationToken ct);
}

public class AzureTenantService : IAzureTenantService
{
    private readonly AccessTokenProviderConfig _config;
    private readonly ILogger<AzureTenantService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AzureTenantService(
        IOptions<AccessTokenProviderConfig> config, 
        ILogger<AzureTenantService> logger,
        HttpClient httpClient = null)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AzureTenantList> GetTenantsAsync(TokenCredential tokenCredential, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Requesting tenants from Azure Management API");
            
            // Get access token for Azure Management API
            var scopes = new[] { $"{_config.AzureManagementBaseUrl}/.default" };
            var requestContext = new TokenRequestContext(scopes);
            var token = await tokenCredential.GetTokenAsync(requestContext, ct).ConfigureAwait(false);
            
            // Prepare the request
            var requestUrl = $"{_config.AzureManagementBaseUrl}tenants?api-version=2022-12-01";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            
            // Send the request
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            // Parse the response using source generation
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var tenantList = JsonSerializer.Deserialize(content, AzureTenantJsonContext.Default.AzureTenantList);
            
            _logger.LogDebug($"Retrieved {tenantList?.Count} tenants from Azure Management API");
            return tenantList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant list from Azure Management API");
            throw;
        }
    }
}