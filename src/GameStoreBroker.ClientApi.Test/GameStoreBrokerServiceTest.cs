// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi.Exceptions;

namespace GameStoreBroker.ClientApi.Test
{
    [TestClass]
    public class GameStoreBrokerServiceTest
    {
        private const string TestBigId = "TestBigId";
        private const string TestProductId = "TestProductId";

        private const string TestUnauthorizedBigId = "TestUnauthorizedBigId";
        private const string TestUnauthorizedProductId = "TestUnauthorizedProductId";

        private GameProduct _testProduct;
        private GameStoreBrokerService _gameStoreBrokerService;
        private AadAuthInfo _aadAuthInfo;

        [TestInitialize]
        public void Initialize()
        {
            _testProduct = new GameProduct
            {
                BigId = TestBigId,
                ProductId = TestProductId,
            };

            var logger = new NullLogger<GameStoreBrokerService>();

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

            var sp = new Mock<IServiceProvider>();
            sp.Setup(p => p.GetService(typeof(ILogger<GameStoreBrokerService>))).Returns(logger);
            sp.Setup(p => p.GetService(typeof(IIngestionHttpClient))).Returns(ingestionClient.Object);

            _gameStoreBrokerService = new GameStoreBrokerService(sp.Object);

            _aadAuthInfo = new AadAuthInfo
            {
                TenantId = "TestTenantId",
                ClientId = "TestClientId",
                ClientSecret = "TestClientSecret",
            };
        }

        [TestMethod]
        public async Task GetProductByProductIdTest()
        {
            var productResult = await _gameStoreBrokerService.GetProductByProductIdAsync(_aadAuthInfo, TestProductId, CancellationToken.None);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestProductId, productResult.ProductId);
        }

        [TestMethod]
        public async Task GetProductByBigIdTest()
        {
            var productResult = await _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, TestBigId, CancellationToken.None);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestBigId, productResult.BigId);
        }

        [TestMethod]
        public async Task GetProductByProductIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _gameStoreBrokerService.GetProductByProductIdAsync(_aadAuthInfo, null, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, null, CancellationToken.None));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByProductIdEmptyTest(string productId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _gameStoreBrokerService.GetProductByProductIdAsync(_aadAuthInfo, productId, CancellationToken.None));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByBigIdEmptyTest(string bigId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, bigId, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByProductIdNotFoundTest()
        {
            await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, "ProductIdNotFound", CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdNotFoundTest()
        {
            await Assert.ThrowsExceptionAsync<ProductNotFoundException>(() => _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, "BigIdNotFound", CancellationToken.None));
        }
        
        [TestMethod]
        public async Task GetProductByProductIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _gameStoreBrokerService.GetProductByProductIdAsync(_aadAuthInfo, TestUnauthorizedProductId, CancellationToken.None));
        }

        [TestMethod]
        public async Task GetProductByBigIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _gameStoreBrokerService.GetProductByBigIdAsync(_aadAuthInfo, TestUnauthorizedBigId, CancellationToken.None));
        }
    }
}
