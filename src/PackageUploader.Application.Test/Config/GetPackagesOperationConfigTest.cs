// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class GetPackagesOperationConfigTest
{
    [TestMethod]
    public void Validate_BlankMarketGroupName_DefaultsToDefault()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            MarketGroupName = "  "
        };

        ConfigTestHelper.ValidateConfig(config);

        Assert.AreEqual("default", config.MarketGroupName);
    }

    [TestMethod]
    public void Validate_NullMarketGroupName_DefaultsToDefault()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            MarketGroupName = null
        };

        ConfigTestHelper.ValidateConfig(config);

        Assert.AreEqual("default", config.MarketGroupName);
    }

    [TestMethod]
    public void Validate_NonBlankMarketGroupName_Preserved()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            MarketGroupName = "custom-group"
        };

        ConfigTestHelper.ValidateConfig(config);

        Assert.AreEqual("custom-group", config.MarketGroupName);
    }
}
