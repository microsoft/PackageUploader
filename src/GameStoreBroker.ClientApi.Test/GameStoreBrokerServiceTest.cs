// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Client.Ingestion;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Client.Xfus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.ClientApi.Test
{
    [TestClass]
    public class PackageUploaderServiceTest
    {
        private const string TestBigId = "TestBigId";
        private const string TestProductId = "TestProductId";

        private const string TestUnauthorizedBigId = "TestUnauthorizedBigId";
        private const string TestUnauthorizedProductId = "TestUnauthorizedProductId";

        private GameProduct _testProduct;
        private PackageUploaderService _PackageUploaderService;

        [TestInitialize]
        public void Initialize()
        {
            _testProduct = new GameProduct
            {
                BigId = TestBigId,
                ProductId = TestProductId,
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

            _PackageUploaderService = new PackageUploaderService(ingestionClient.Object, xfusUploader.Object, logger);
        }

        [TestMethod]
        public async Task GetProductByProductIdTest()
        {
            var productResult = await _PackageUploaderService.GetProductByProductIdAsync(TestProductId, CancellationToken.None);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestProductId, productResult.ProductId);
        }

        [TestMethod]
        public async Task GetProductByBigIdTest()
        {
            var productResult = await _PackageUploaderService.GetProductByBigIdAsync(TestBigId, CancellationToken.None);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestBigId, productResult.BigId);
        }

        [TestMethod]
        public async Task GetProductByProductIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _PackageUploaderService.GetProductByProductIdAsync(null, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _PackageUploaderService.GetProductByBigIdAsync(null, CancellationToken.None));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByProductIdEmptyTest(string productId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _PackageUploaderService.GetProductByProductIdAsync(productId, CancellationToken.None));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByBigIdEmptyTest(string bigId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _PackageUploaderService.GetProductByBigIdAsync(bigId, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByProductIdNotFoundTest()
        {
            await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _PackageUploaderService.GetProductByBigIdAsync("ProductIdNotFound", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdNotFoundTest()
        {
            await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _PackageUploaderService.GetProductByBigIdAsync("BigIdNotFound", CancellationToken.None));
        }
        
        [TestMethod]
        public async Task GetProductByProductIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _PackageUploaderService.GetProductByProductIdAsync(TestUnauthorizedProductId, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _PackageUploaderService.GetProductByBigIdAsync(TestUnauthorizedBigId, CancellationToken.None));
        }
    }
}
