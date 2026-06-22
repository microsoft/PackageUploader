// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PackageUploader.IntegrationTest.Infrastructure;

/// <summary>Base class for integration tests; applies the <c>Integration</c> category and provides a host factory.</summary>
[TestCategory(Category)]
public abstract class IntegrationTestBase
{
    public const string Category = "Integration";

    private protected static PackageUploaderTestHost CreateHost(
        Action<MockHttpMessageHandler>? configureIngestion = null) => new(configureIngestion);
}
