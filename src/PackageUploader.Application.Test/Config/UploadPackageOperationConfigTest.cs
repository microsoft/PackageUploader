// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class UploadPackageOperationConfigTest
{
    [TestMethod]
    public void Validate_AvailabilityDateEnabledWithoutEffectiveDate_ReturnsError()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = null }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("AvailabilityDate"), results);
    }

    [TestMethod]
    public void Validate_AvailabilityDateEnabledWithEffectiveDate_NoAvailabilityError()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("AvailabilityDate"), results);
    }

    [TestMethod]
    public void Validate_BlankMarketGroupName_DefaultsToDefault()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            MarketGroupName = ""
        };

        ConfigTestHelper.ValidateConfig(config);

        Assert.AreEqual("default", config.MarketGroupName);
    }
}
