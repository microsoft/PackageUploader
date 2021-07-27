using GameStoreBroker.ClientApi.Client.Ingestion;
using GameStoreBroker.ClientApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
            ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestProductId))
                .ReturnsAsync(_testProduct);
            ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestBigId))
                .ReturnsAsync(_testProduct);

            ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(TestUnauthorizedProductId))
                .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));
            ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(TestUnauthorizedBigId))
                .ThrowsAsync(new HttpRequestException(string.Empty, null, HttpStatusCode.Unauthorized));

            ingestionClient.Setup(p => p.GetGameProductByLongIdAsync(It.IsNotIn(TestProductId, TestUnauthorizedProductId)))
                .ReturnsAsync((GameProduct)null);
            ingestionClient.Setup(p => p.GetGameProductByBigIdAsync(It.IsNotIn(TestBigId, TestUnauthorizedBigId)))
                .ReturnsAsync((GameProduct)null);

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
            var productResult = await _gameStoreBrokerService.GetProductByProductId(_aadAuthInfo, TestProductId);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestProductId, productResult.ProductId);
        }

        [TestMethod]
        public async Task GetProductByBigIdTest()
        {
            var productResult = await _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, TestBigId);

            Assert.IsNotNull(productResult);
            Assert.AreEqual(TestBigId, productResult.BigId);
        }

        [TestMethod]
        public async Task GetProductByProductIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _gameStoreBrokerService.GetProductByProductId(_aadAuthInfo, null));
        }
        [TestMethod]
        public async Task GetProductByBigIdNullTest()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, null));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByProductIdEmptyTest(string productId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _gameStoreBrokerService.GetProductByProductId(_aadAuthInfo, productId));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("     ")]
        public async Task GetProductByBigIdEmptyTest(string bigId)
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, bigId));
        }

        [TestMethod]
        public async Task GetProductByProductIdNotFoundTest()
        {
            var productResult = await _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, "ProductIdNotFound");
            Assert.IsNull(productResult);
        }

        [TestMethod]
        public async Task GetProductByBigIdNotFoundTest()
        {
            var productResult = await _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, "BigIdNotFound");
            Assert.IsNull(productResult);
        }
        
        [TestMethod]
        public async Task GetProductByProductIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _gameStoreBrokerService.GetProductByProductId(_aadAuthInfo, TestUnauthorizedProductId));
        }

        [TestMethod]
        public async Task GetProductByBigIdUnauthorizedTest()
        {
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _gameStoreBrokerService.GetProductByBigId(_aadAuthInfo, TestUnauthorizedBigId));
        }
    }
}
