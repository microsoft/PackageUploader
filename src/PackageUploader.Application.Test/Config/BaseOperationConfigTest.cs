// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Test.Config;

[TestClass]
public class BaseOperationConfigTest
{
    [TestMethod]
    public void Validate_WrongOperationName_ReturnsError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "WrongName",
            ProductId = "product-123"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("OperationName"), results);
    }

    [TestMethod]
    public void Validate_CorrectOperationName_NoOperationNameError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            ProductId = "product-123"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("OperationName"), results);
    }

    [TestMethod]
    public void Validate_NeitherProductIdNorBigId_ReturnsError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            ProductId = null,
            BigId = null
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.MemberNames.Contains("ProductId") && r.MemberNames.Contains("BigId"), results);
    }

    [TestMethod]
    public void Validate_BothProductIdAndBigId_ReturnsError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            ProductId = "product-123",
            BigId = "big-123"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.Contains(r => r.ErrorMessage!.Contains("Only one"), results);
    }

    [TestMethod]
    public void Validate_OnlyProductId_NoIdError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            ProductId = "product-123"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("ProductId") || r.MemberNames.Contains("BigId"), results);
    }

    [TestMethod]
    public void Validate_OnlyBigId_NoIdError()
    {
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            BigId = "big-123"
        };

        var results = ConfigTestHelper.ValidateConfig(config);

        Assert.DoesNotContain(r => r.MemberNames.Contains("ProductId") || r.MemberNames.Contains("BigId"), results);
    }
}
