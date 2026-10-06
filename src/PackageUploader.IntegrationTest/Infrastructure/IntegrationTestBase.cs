// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>Base class for integration tests; applies the <c>Integration</c> category and provides a host factory.</summary>
[TestCategory(Category)]
public abstract class IntegrationTestBase
{
    public const string Category = "Integration";

    /// <summary>Creates a host wired to live WireMock.Net fakes of the Ingestion API and XFUS.</summary>
    private protected static MockServerTestHost CreateMockServerHost() => new();
}
