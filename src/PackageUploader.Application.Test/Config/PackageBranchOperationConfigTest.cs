// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class PackageBranchOperationConfigTest
{
    [TestMethod]
    public void Validate_NeitherBranchNorFlight_ReturnsError()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = null,
            FlightName = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("BranchFriendlyName") && r.MemberNames.Contains("FlightName"), results);
    }

    [TestMethod]
    public void Validate_BothBranchAndFlight_ReturnsError()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            FlightName = "flight-1"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("Only one"), results);
    }

    [TestMethod]
    public void Validate_OnlyBranch_NoBranchError()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("BranchFriendlyName") || r.MemberNames.Contains("FlightName"), results);
    }

    [TestMethod]
    public void Validate_OnlyFlight_NoFlightError()
    {
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "product-123",
            FlightName = "flight-1"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("BranchFriendlyName") || r.MemberNames.Contains("FlightName"), results);
    }
}
