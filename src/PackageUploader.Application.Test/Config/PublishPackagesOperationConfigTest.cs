// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class PublishPackagesOperationConfigTest
{
    [TestMethod]
    public void Validate_FlightNameOnly_NoPublishError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            FlightName = "flight-1",
            BranchFriendlyName = null,
            DestinationSandboxName = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("DestinationSandboxName"), results);
    }

    [TestMethod]
    public void Validate_BranchAndSandbox_NoPublishError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            FlightName = null,
            BranchFriendlyName = "main",
            DestinationSandboxName = "XDKS.1"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r =>
            r.MemberNames.Contains("FlightName") &&
            r.MemberNames.Contains("BranchFriendlyName") &&
            r.MemberNames.Contains("DestinationSandboxName"), results);
    }

    [TestMethod]
    public void Validate_NeitherFlightNorBranchSandbox_ReturnsError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            FlightName = null,
            BranchFriendlyName = null,
            DestinationSandboxName = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("FlightName"), results);
    }

    [TestMethod]
    public void Validate_FlightAndBranchAndSandbox_ReturnsError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            FlightName = "flight-1",
            BranchFriendlyName = "main",
            DestinationSandboxName = "XDKS.1"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("FlightName"), results);
    }

    [TestMethod]
    public void Validate_DestinationSandboxRetail_ReturnsError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationSandboxName = "RETAIL"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("RETAIL"), results);
    }

    [TestMethod]
    public void Validate_DestinationSandboxRetailCaseInsensitive_ReturnsError()
    {
        var config = new PublishPackagesOperationConfig
        {
            OperationName = "PublishPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationSandboxName = "retail"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("RETAIL"), results);
    }
}
