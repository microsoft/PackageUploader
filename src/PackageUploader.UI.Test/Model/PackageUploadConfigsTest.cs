using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.Reflection;

namespace PackageUploader.UI.Test.Model
{
    [TestClass]
    public class PackageUploadConfigsTest
    {
        #region UploadConfig Tests

        [TestMethod]
        public void UploadConfig_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var config = new UploadConfig();

            // Assert
            Assert.AreEqual("UploadXvcPackage", config.operationName);
            Assert.IsNull(config.aadAuthInfo);
            Assert.IsNull(config.productId);
            Assert.AreEqual(string.Empty, config.bigId);
            Assert.AreEqual("Main", config.branchFriendlyName);
            Assert.IsNull(config.flightName);
            Assert.AreEqual("default", config.marketGroupName);
            Assert.AreEqual(string.Empty, config.packageFilePath);
            Assert.IsTrue(config.deltaUpload);
            Assert.IsNotNull(config.gameAssets);
            Assert.AreEqual(60, config.minutesToWaitForProcessing);
            Assert.IsNull(config.availabilityDate);
            Assert.IsNull(config.preDownloadDate);
            Assert.IsNotNull(config.uploadConfig);
        }

        [TestMethod]
        public void UploadConfig_PropertiesSetAndGet()
        {
            // Arrange
            var config = new UploadConfig();
            var authInfo = new AadAuthInfo();

            // Act
            config.aadAuthInfo = authInfo;
            config.productId = "TestId";
            config.bigId = "TestBigId";
            config.branchFriendlyName = "TestBranch";
            config.flightName = "TestFlight";
            config.marketGroupName = "TestMarket";
            config.packageFilePath = "C:\\test\\package.xvc";
            config.deltaUpload = false;
            config.minutesToWaitForProcessing = 30;
            config.availabilityDate = new AvailabilityDate();
            config.preDownloadDate = new AvailabilityDate();

            // Assert
            Assert.AreEqual(authInfo, config.aadAuthInfo);
            Assert.AreEqual("TestId", config.productId);
            Assert.AreEqual("TestBigId", config.bigId);
            Assert.AreEqual("TestBranch", config.branchFriendlyName);
            Assert.AreEqual("TestFlight", config.flightName);
            Assert.AreEqual("TestMarket", config.marketGroupName);
            Assert.AreEqual("C:\\test\\package.xvc", config.packageFilePath);
            Assert.IsFalse(config.deltaUpload);
            Assert.AreEqual(30, config.minutesToWaitForProcessing);
            Assert.IsNotNull(config.availabilityDate);
            Assert.IsNotNull(config.preDownloadDate);
        }

        #endregion

        #region AadAuthInfo Tests

        [TestMethod]
        public void AadAuthInfo_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var authInfo = new AadAuthInfo();

            // Assert
            Assert.AreEqual(string.Empty, authInfo.tenantId);
            Assert.AreEqual(string.Empty, authInfo.clientId);
            Assert.AreEqual(string.Empty, authInfo.certificateThumbprint);
            Assert.IsNull(authInfo.certificateStore);
            Assert.IsNull(authInfo.certificateLocation);
        }

        [TestMethod]
        public void AadAuthInfo_PropertiesSetAndGet()
        {
            // Arrange
            var authInfo = new AadAuthInfo();

            // Act
            authInfo.tenantId = "TestTenant";
            authInfo.clientId = "TestClient";
            authInfo.certificateThumbprint = "TestThumbprint";
            authInfo.certificateStore = "TestStore";
            authInfo.certificateLocation = "TestLocation";

            // Assert
            Assert.AreEqual("TestTenant", authInfo.tenantId);
            Assert.AreEqual("TestClient", authInfo.clientId);
            Assert.AreEqual("TestThumbprint", authInfo.certificateThumbprint);
            Assert.AreEqual("TestStore", authInfo.certificateStore);
            Assert.AreEqual("TestLocation", authInfo.certificateLocation);
        }

        #endregion

        #region GameAssets Tests

        [TestMethod]
        public void GameAssets_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var assets = new GameAssets();

            // Assert
            Assert.AreEqual(string.Empty, assets.ekbFilePath);
            Assert.AreEqual(string.Empty, assets.subValFilePath);
            Assert.AreEqual(string.Empty, assets.symbolsFilePath);
            Assert.AreEqual(string.Empty, assets.discLayoutFilePath);
        }

        [TestMethod]
        public void GameAssets_PropertiesSetAndGet()
        {
            // Arrange
            var assets = new GameAssets();

            // Act
            assets.ekbFilePath = "C:\\test\\ekb.xml";
            assets.subValFilePath = "C:\\test\\subval.xml";
            assets.symbolsFilePath = "C:\\test\\symbols.pdb";
            assets.discLayoutFilePath = "C:\\test\\disc.xml";

            // Assert
            Assert.AreEqual("C:\\test\\ekb.xml", assets.ekbFilePath);
            Assert.AreEqual("C:\\test\\subval.xml", assets.subValFilePath);
            Assert.AreEqual("C:\\test\\symbols.pdb", assets.symbolsFilePath);
            Assert.AreEqual("C:\\test\\disc.xml", assets.discLayoutFilePath);
        }

        #endregion

        #region AvailabilityDate Tests

        [TestMethod]
        public void AvailabilityDate_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var date = new AvailabilityDate();

            // Assert
            Assert.AreEqual(false, date.isEnabled);
            Assert.IsNull(date.effectiveDate);
        }

        [TestMethod]
        public void AvailabilityDate_PropertiesSetAndGet()
        {
            // Arrange
            var date = new AvailabilityDate();
            var testDate = new DateTime(2023, 5, 15);

            // Act
            date.isEnabled = true;
            date.effectiveDate = testDate;

            // Assert
            Assert.IsTrue(date.isEnabled);
            Assert.AreEqual(testDate, date.effectiveDate);
        }

        #endregion

        #region UploadClientConfig Tests

        [TestMethod]
        public void UploadClientConfig_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var clientConfig = new UploadClientConfig();

            // Assert
            Assert.AreEqual(300000, clientConfig.httpTimeoutMs);
            Assert.AreEqual(300000, clientConfig.httpUploadTimeoutMs);
            Assert.AreEqual(24, clientConfig.maxParallelism);
            Assert.AreEqual(-1, clientConfig.defaultConnectionLimit);
            Assert.IsFalse(clientConfig.expect100Continue);
            Assert.IsFalse(clientConfig.useNagleAlgorithm);
        }

        [TestMethod]
        public void UploadClientConfig_PropertiesSetAndGet()
        {
            // Arrange
            var clientConfig = new UploadClientConfig();

            // Act
            clientConfig.httpTimeoutMs = 600000;
            clientConfig.httpUploadTimeoutMs = 900000;
            clientConfig.maxParallelism = 32;
            clientConfig.defaultConnectionLimit = 100;
            clientConfig.expect100Continue = true;
            clientConfig.useNagleAlgorithm = true;

            // Assert
            Assert.AreEqual(600000, clientConfig.httpTimeoutMs);
            Assert.AreEqual(900000, clientConfig.httpUploadTimeoutMs);
            Assert.AreEqual(32, clientConfig.maxParallelism);
            Assert.AreEqual(100, clientConfig.defaultConnectionLimit);
            Assert.IsTrue(clientConfig.expect100Continue);
            Assert.IsTrue(clientConfig.useNagleAlgorithm);
        }

        #endregion

        #region GetProductConfig Tests

        [TestMethod]
        public void GetProductConfig_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var config = new GetProductConfig();

            // Assert
            Assert.AreEqual("GetProduct", config.operationName);
            Assert.IsNull(config.aadAuthInfo);
            Assert.IsNull(config.productId);
            Assert.AreEqual(string.Empty, config.bigId);
        }

        [TestMethod]
        public void GetProductConfig_PropertiesSetAndGet()
        {
            // Arrange
            var config = new GetProductConfig();
            var authInfo = new AadAuthInfo();

            // Act
            config.aadAuthInfo = authInfo;
            config.productId = "TestId";
            config.bigId = "TestBigId";

            // Assert
            Assert.AreEqual(authInfo, config.aadAuthInfo);
            Assert.AreEqual("TestId", config.productId);
            Assert.AreEqual("TestBigId", config.bigId);
        }

        #endregion

        #region GetProductResponse Tests

        [TestMethod]
        public void GetProductResponse_HasCorrectDefaultValues()
        {
            // Arrange & Act
            var response = new GetProductResponse();

            // Assert
            Assert.AreEqual(string.Empty, response.productId);
            Assert.AreEqual(string.Empty, response.productName);
            Assert.AreEqual(string.Empty, response.bigId);
            Assert.AreEqual(0, response.branchFriendlyNames.Length);
            Assert.AreEqual(0, response.flightNames.Length);
        }

        [TestMethod]
        public void GetProductResponse_PropertiesSetAndGet()
        {
            // Arrange
            var response = new GetProductResponse();
            var branches = new[] { "Branch1", "Branch2" };
            var flights = new[] { "Flight1", "Flight2" };

            // Act
            response.productId = "TestId";
            response.productName = "TestName";
            response.bigId = "TestBigId";
            response.branchFriendlyNames = branches;
            response.flightNames = flights;

            // Assert
            Assert.AreEqual("TestId", response.productId);
            Assert.AreEqual("TestName", response.productName);
            Assert.AreEqual("TestBigId", response.bigId);
            CollectionAssert.AreEqual(branches, response.branchFriendlyNames);
            CollectionAssert.AreEqual(flights, response.flightNames);
        }

        #endregion
    }
}