// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class UploadXvcPackageOperationConfigTest
{
    [TestMethod]
    public void Validate_PreDownloadDateEnabledWithoutEffectiveDate_ReturnsError()
    {
        var config = new TestUploadXvcPackageOperationConfig
        {
            OperationName = "UploadXvcPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            GameAssets = new GameAssets { EkbFilePath = "ekb", SubValFilePath = "sub" },
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(2) },
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = null }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("PreDownloadDate"), results);
    }

    [TestMethod]
    public void Validate_PreDownloadDateWithoutAvailabilityDate_ReturnsError()
    {
        var config = new TestUploadXvcPackageOperationConfig
        {
            OperationName = "UploadXvcPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            GameAssets = new GameAssets { EkbFilePath = "ekb", SubValFilePath = "sub" },
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) },
            AvailabilityDate = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("needs AvailabilityDate"), results);
    }

    [TestMethod]
    public void Validate_PreDownloadDateAfterAvailabilityDate_ReturnsError()
    {
        var config = new TestUploadXvcPackageOperationConfig
        {
            OperationName = "UploadXvcPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            GameAssets = new GameAssets { EkbFilePath = "ekb", SubValFilePath = "sub" },
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) },
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(5) }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("needs to be before"), results);
    }

    [TestMethod]
    public void Validate_PreDownloadDateBeforeAvailabilityDate_NoPreDownloadError()
    {
        var config = new TestUploadXvcPackageOperationConfig
        {
            OperationName = "UploadXvcPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            GameAssets = new GameAssets { EkbFilePath = "ekb", SubValFilePath = "sub" },
            AvailabilityDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(5) },
            PreDownloadDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("PreDownloadDate"), results);
    }
}
