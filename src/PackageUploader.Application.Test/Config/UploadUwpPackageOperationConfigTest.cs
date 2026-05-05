// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class UploadUwpPackageOperationConfigTest
{
    [TestMethod]
    public void Validate_MandatoryDateEnabledWithoutEffectiveDate_ReturnsError()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            MandatoryDate = new GamePackageDate { IsEnabled = true, EffectiveDate = null }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("MandatoryDate"), results);
    }

    [TestMethod]
    public void Validate_MandatoryDateEnabledWithEffectiveDate_NoMandatoryError()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            MandatoryDate = new GamePackageDate { IsEnabled = true, EffectiveDate = DateTime.UtcNow.AddDays(1) }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("MandatoryDate"), results);
    }

    [TestMethod]
    public void Validate_MandatoryDateDisabled_NoMandatoryError()
    {
        var config = new TestUploadUwpPackageOperationConfig
        {
            OperationName = "UploadUwpPackage",
            ProductId = "product-123",
            BranchFriendlyName = "main",
            PackageFilePath = "test.msixvc",
            MandatoryDate = new GamePackageDate { IsEnabled = false }
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(static r => r.MemberNames.Contains("MandatoryDate"), results);
    }
}
