using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using System;
using System.IO;

namespace PackageUploader.UI.Test
{
    [TestClass]
    public class PackageModelProviderTest
    {
        private PackageModelProvider _packageModelProvider;

        [TestInitialize]
        public void Setup()
        {
            _packageModelProvider = new PackageModelProvider();
        }

        [TestMethod]
        public void Constructor_Should_InitializeEmptyPackageModel()
        {
            // Assert
            Assert.IsNotNull(_packageModelProvider.Package);
            Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.PackageFilePath));
            Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.GameConfigFilePath));
            Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.PackageType));
            Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.PackageSize));
        }

        [TestMethod]
        public void Package_Should_RetainValuesWhenSet()
        {
            // Arrange
            var packageModel = new PackageModel
            {
                PackageFilePath = "C:\\packages\\game.xvc",
                GameConfigFilePath = "C:\\game\\MicrosoftGame.config",
                PackageType = "Xbox",
                PackageSize = "5.2 GB"
            };

            // Act
            _packageModelProvider.Package = packageModel;

            // Assert
            Assert.AreEqual("C:\\packages\\game.xvc", _packageModelProvider.Package.PackageFilePath);
            Assert.AreEqual("C:\\game\\MicrosoftGame.config", _packageModelProvider.Package.GameConfigFilePath);
            Assert.AreEqual("Xbox", _packageModelProvider.Package.PackageType);
            Assert.AreEqual("5.2 GB", _packageModelProvider.Package.PackageSize);
        }

        [TestMethod]
        public void Package_WhenSetWithNullValue_Should_ReplaceWithEmptyModel()
        {
            // Act
            _packageModelProvider.Package = null;

            // Assert
            Assert.IsNotNull(_packageModelProvider.Package);
            Assert.IsTrue(string.IsNullOrEmpty(_packageModelProvider.Package.PackageFilePath));
        }

        [TestMethod]
        public void PackageChanged_Should_FireWhenPackageIsUpdated()
        {
            // Arrange
            bool eventFired = false;
            _packageModelProvider.PropertyChanged += (sender, args) => { eventFired = true; };

            // Act
            _packageModelProvider.Package = new PackageModel { PackageFilePath = "C:\\newpackage.xvc" };

            // Assert
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void PackageChanged_Should_NotFireWhenSamePackageIsSetAgain()
        {
            // Arrange
            var package = new PackageModel { PackageFilePath = "C:\\package.xvc" };
            _packageModelProvider.Package = package;

            bool eventFired = false;
            _packageModelProvider.PropertyChanged += (sender, args) => { eventFired = true; };

            // Act
            _packageModelProvider.Package = package; // Same instance

            // Assert
            Assert.IsFalse(eventFired);
        }
    }
}