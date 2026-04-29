// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Test.Config;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.Application.Test.Extensions;

[TestClass]
public class PackageUploaderExtensionsTest
{
    private readonly Mock<IPackageUploaderService> _serviceMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static GameProduct CreateGameProduct(string productId = "prod-1", string bigId = "big-1", string productName = "Test")
    {
        var product = (GameProduct)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameProduct));
        typeof(GameProduct).GetProperty("ProductId")!.SetValue(product, productId);
        typeof(GameProduct).GetProperty("BigId")!.SetValue(product, bigId);
        typeof(GameProduct).GetProperty("ProductName")!.SetValue(product, productName);
        return product;
    }

    private static GamePackageBranch CreateGamePackageBranch(string name = "main")
    {
        var branch = (GamePackageBranch)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GamePackageBranch));
        typeof(GamePackageBranch).GetProperty("Name")!.SetValue(branch, name);
        return branch;
    }

    private static GamePackageFlight CreateGamePackageFlight(string name = "flight-1")
    {
        var flight = (GamePackageFlight)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GamePackageFlight));
        typeof(GamePackageFlight).GetProperty("Name")!.SetValue(flight, name);
        return flight;
    }

    #region GetProductAsync

    [TestMethod]
    public async Task GetProductAsync_WithBigId_CallsGetProductByBigId()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            BigId = "big-123"
        };
        _serviceMock.Setup(s => s.GetProductByBigIdAsync("big-123", _ct)).ReturnsAsync(testProduct);

        var result = await _serviceMock.Object.GetProductAsync(config, _ct);

        Assert.AreEqual(testProduct, result);
        _serviceMock.Verify(s => s.GetProductByBigIdAsync("big-123", _ct), Times.Once);
    }

    [TestMethod]
    public async Task GetProductAsync_WithProductId_CallsGetProductByProductId()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            ProductId = "prod-123"
        };
        _serviceMock.Setup(s => s.GetProductByProductIdAsync("prod-123", _ct)).ReturnsAsync(testProduct);

        var result = await _serviceMock.Object.GetProductAsync(config, _ct);

        Assert.AreEqual(testProduct, result);
        _serviceMock.Verify(s => s.GetProductByProductIdAsync("prod-123", _ct), Times.Once);
    }

    [TestMethod]
    public async Task GetProductAsync_BigIdPrioritizedOverProductId()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetProductOperationConfig
        {
            OperationName = "GetProduct",
            BigId = "big-123",
            ProductId = "prod-123"
        };
        _serviceMock.Setup(s => s.GetProductByBigIdAsync("big-123", _ct)).ReturnsAsync(testProduct);

        await _serviceMock.Object.GetProductAsync(config, _ct);

        _serviceMock.Verify(s => s.GetProductByBigIdAsync("big-123", _ct), Times.Once);
        _serviceMock.Verify(s => s.GetProductByProductIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductAsync_NeitherIdSet_ThrowsException()
    {
        var config = new TestGetProductOperationConfig { OperationName = "GetProduct" };

        await Assert.ThrowsExactlyAsync<Exception>(() => _serviceMock.Object.GetProductAsync(config, _ct));
    }

    [TestMethod]
    public async Task GetProductAsync_NullService_ThrowsArgumentNullException()
    {
        var config = new TestGetProductOperationConfig { OperationName = "GetProduct", ProductId = "prod-1" };

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            PackageUploaderExtensions.GetProductAsync(null!, config, _ct));
    }

    [TestMethod]
    public async Task GetProductAsync_NullConfig_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            _serviceMock.Object.GetProductAsync(null!, _ct));
    }

    #endregion

    #region GetGamePackageBranch

    [TestMethod]
    public async Task GetGamePackageBranch_WithBranch_CallsGetByFriendlyName()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "prod-1",
            BranchFriendlyName = "main"
        };
        var branch = CreateGamePackageBranch("main");
        _serviceMock.Setup(s => s.GetPackageBranchByFriendlyNameAsync(testProduct, "main", _ct))
            .ReturnsAsync(branch);

        var result = await _serviceMock.Object.GetGamePackageBranch(testProduct, config, _ct);

        Assert.AreEqual(branch, result);
    }

    [TestMethod]
    public async Task GetGamePackageBranch_WithFlight_CallsGetByFlightName()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "prod-1",
            FlightName = "flight-1"
        };
        var flight = CreateGamePackageFlight("flight-1");
        _serviceMock.Setup(s => s.GetPackageFlightByFlightNameAsync(testProduct, "flight-1", _ct))
            .ReturnsAsync(flight);

        var result = await _serviceMock.Object.GetGamePackageBranch(testProduct, config, _ct);

        Assert.AreEqual(flight, result);
    }

    [TestMethod]
    public async Task GetGamePackageBranch_NeitherSet_ThrowsException()
    {
        var testProduct = CreateGameProduct();
        var config = new TestGetPackagesOperationConfig
        {
            OperationName = "GetPackages",
            ProductId = "prod-1"
        };

        await Assert.ThrowsExactlyAsync<Exception>(() =>
            _serviceMock.Object.GetGamePackageBranch(testProduct, config, _ct));
    }

    #endregion

    #region GetDestinationGamePackageBranch

    [TestMethod]
    public async Task GetDestinationGamePackageBranch_WithDestBranch_CallsGetByFriendlyName()
    {
        var testProduct = CreateGameProduct();
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "prod-1",
            BranchFriendlyName = "main",
            DestinationBranchFriendlyName = "dest-branch"
        };
        var branch = CreateGamePackageBranch("dest-branch");
        _serviceMock.Setup(s => s.GetPackageBranchByFriendlyNameAsync(testProduct, "dest-branch", _ct))
            .ReturnsAsync(branch);

        var result = await _serviceMock.Object.GetDestinationGamePackageBranch(testProduct, config, _ct);

        Assert.AreEqual(branch, result);
    }

    [TestMethod]
    public async Task GetDestinationGamePackageBranch_WithDestFlight_CallsGetByFlightName()
    {
        var testProduct = CreateGameProduct();
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "prod-1",
            BranchFriendlyName = "main",
            DestinationFlightName = "dest-flight"
        };
        var flight = CreateGamePackageFlight("dest-flight");
        _serviceMock.Setup(s => s.GetPackageFlightByFlightNameAsync(testProduct, "dest-flight", _ct))
            .ReturnsAsync(flight);

        var result = await _serviceMock.Object.GetDestinationGamePackageBranch(testProduct, config, _ct);

        Assert.AreEqual(flight, result);
    }

    [TestMethod]
    public async Task GetDestinationGamePackageBranch_NeitherSet_ThrowsException()
    {
        var testProduct = CreateGameProduct();
        var config = new TestImportPackagesOperationConfig
        {
            OperationName = "ImportPackages",
            ProductId = "prod-1",
            BranchFriendlyName = "main"
        };

        await Assert.ThrowsExactlyAsync<Exception>(() =>
            _serviceMock.Object.GetDestinationGamePackageBranch(testProduct, config, _ct));
    }

    #endregion
}
