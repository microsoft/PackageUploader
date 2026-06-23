// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>Test <see cref="IAccessTokenProvider"/> that returns a static fake token so the mock suite needs no real credentials.</summary>
internal sealed class FakeAccessTokenProvider : IAccessTokenProvider
{
    public const string FakeToken = "fake-integration-test-token";

    public Task<IngestionAccessToken> GetTokenAsync(CancellationToken ct) =>
        Task.FromResult(new IngestionAccessToken
        {
            AccessToken = FakeToken,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
        });
}
