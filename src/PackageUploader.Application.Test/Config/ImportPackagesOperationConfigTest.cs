// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class ImportPackagesOperationConfigTest
{
    [TestMethod]
    public void Validate_NeitherDestinationBranchNorFlight_ReturnsError()
    {
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = null,
            DestinationFlightName = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("DestinationBranchFriendlyName"), results);
    }

    [TestMethod]
    public void Validate_BothDestinationBranchAndFlight_ReturnsError()
    {
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = "dest-branch",
            DestinationFlightName = "dest-flight"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("Only one"), results);
    }

    [TestMethod]
    public void Validate_OnlyDestinationBranch_NoDestinationError()
    {
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = "dest-branch"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("DestinationBranchFriendlyName") || r.MemberNames.Contains("DestinationFlightName"), results);
    }

    [TestMethod]
    public void Validate_PreDownloadDateWithoutAvailabilityDate_ReturnsError()
    {
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = "dest-branch",
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) },
            AvailabilityDate = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("needs AvailabilityDate"), results);
    }

    [TestMethod]
    public void Validate_PreDownloadDateAfterAvailabilityDate_ReturnsError()
    {
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = "dest-branch",
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) },
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(5) }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("needs to be before"), results);
    }
}
