// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus.Uploader;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PackageUploader.ClientApi.Client.Ingestion.Client;
using PackageUploader.ClientApi.Client.Ingestion.Models.Internal;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net.Http.Json;
using PackageUploader.ClientApi.Client.Ingestion.Mappers;

namespace PackageUploader.ClientApi.Test;

[TestClass]
public class PackageUploaderServiceTest
{
    private const string TestBigId = "TestBigId";
    private const string TestProductId = "TestProductId";

    private const string TestMovedPackageId = "TestMovedPackageId";
    private const string TestMovedPackageToId = "TestMovedPackageToId";

    private const string TestUnauthorizedBigId = "TestUnauthorizedBigId";
    private const string TestUnauthorizedProductId = "TestUnauthorizedProductId";

    private GameProduct _testProduct;
    private PackageUploaderService _packageUploaderService;
    private IngestionRedirectPackage _redirectPackage;
    private IngestionGamePackage _movedPackage;
    private IngestionHttpClient _ingestionClient;

    [TestInitialize]
    public void Initialize()
    {
        _testProduct = new GameProduct
        {
            BigId = TestBigId,
            ProductId = TestProductId,
        };

        _redirectPackage = new IngestionRedirectPackage
        {
            Id = TestMovedPackageId,
            ToId = TestMovedPackageToId,
            ProcessingState = GamePackageState.Processed.ToString()
        };
        _movedPackage = new IngestionGamePackage
        {
            Id = TestMovedPackageToId,
            State = GamePackageState.Processed.ToString(),
        };

        var logger = new NullLogger<PackageUploaderService>();

        var ingestionClient = new Mock<IIngestionHttpClient>();
        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProduct);
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestBigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProduct);

        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestUnauthorizedProductId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestUnauthorizedBigId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));

        ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(It.IsNotIn(TestProductId, TestUnauthorizedProductId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductNotFoundException(string.Empty));
        ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(It.IsNotIn(TestBigId, TestUnauthorizedBigId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductNotFoundException(string.Empty));

        var xfusUploader = new Mock<IXfusUploader>();

        _packageUploaderService = new PackageUploaderService(ingestionClient.Object, xfusUploader.Object, logger);

        var httpClient = new Mock<HttpClient>();
        // Helpful References:
        // https://stackoverflow.com/questions/36425008/mocking-httpclient-in-unit-tests
        //https://stackoverflow.com/questions/60094386/moq-verify-on-a-mocked-httpclienthandler-cant-access-the-content-object-becau
        httpClient.Setup(p => p.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.ToString().Contains($"products/{TestProductId}/packages/{TestMovedPackageId}")), 
                                          It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.MovedPermanently) { Content = JsonContent.Create<IngestionRedirectPackage>(_redirectPackage) });
        httpClient.Setup(p => p.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.ToString().Contains($"products/{TestProductId}/packages/{TestMovedPackageToId}")),
                                          It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create<IngestionGamePackage>(_movedPackage) });

        var loggerIngestionClient = new NullLogger<IngestionHttpClient>();
        _ingestionClient = new IngestionHttpClient(loggerIngestionClient, httpClient.Object, null);
    }

    [TestMethod]
    public async Task GetProductByProductIdTest()
    {
        var productResult = await _packageUploaderService.GetProductByProductIdAsync(TestProductId, CancellationToken.None);

        Assert.IsNotNull(productResult);
        Assert.AreEqual(TestProductId, productResult.ProductId);
    }

    [TestMethod]
    public async Task GetProductByBigIdTest()
    {
        var productResult = await _packageUploaderService.GetProductByBigIdAsync(TestBigId, CancellationToken.None);

        Assert.IsNotNull(productResult);
        Assert.AreEqual(TestBigId, productResult.BigId);
    }

    [TestMethod]
    public async Task GetProductByProductIdNullTest()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _packageUploaderService.GetProductByProductIdAsync(null, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdNullTest()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _packageUploaderService.GetProductByBigIdAsync(null, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("     ")]
    public async Task GetProductByProductIdEmptyTest(string productId)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _packageUploaderService.GetProductByProductIdAsync(productId, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("     ")]
    public async Task GetProductByBigIdEmptyTest(string bigId)
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _packageUploaderService.GetProductByBigIdAsync(bigId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByProductIdNotFoundTest()
    {
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _packageUploaderService.GetProductByBigIdAsync("ProductIdNotFound", CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdNotFoundTest()
    {
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _packageUploaderService.GetProductByBigIdAsync("BigIdNotFound", CancellationToken.None));
    }
        
    [TestMethod]
    public async Task GetProductByProductIdUnauthorizedTest()
    {
        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _packageUploaderService.GetProductByProductIdAsync(TestUnauthorizedProductId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetProductByBigIdUnauthorizedTest()
    {
        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _packageUploaderService.GetProductByBigIdAsync(TestUnauthorizedBigId, CancellationToken.None));
    }

    [TestMethod]
    public async Task GetPackageByIdAsyncRedirectTest()
    {
        var packageResult = await _ingestionClient.GetPackageByIdAsync(TestProductId, TestMovedPackageId, CancellationToken.None);
        Assert.IsNotNull(packageResult);
        Assert.AreEqual(TestMovedPackageToId, packageResult.Id);
        Assert.AreEqual(_movedPackage.State, packageResult.State.ToString());
    }
}