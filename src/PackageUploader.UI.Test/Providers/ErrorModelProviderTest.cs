using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using System;

namespace PackageUploader.UI.Test
{
    [TestClass]
    public class ErrorModelProviderTest
    {
        private ErrorModelProvider _errorModelProvider;

        [TestInitialize]
        public void Setup()
        {
            _errorModelProvider = new ErrorModelProvider();
        }

        [TestMethod]
        public void Constructor_Should_InitializeEmptyErrorModel()
        {
            // Assert
            Assert.IsNotNull(_errorModelProvider.Error);
            Assert.IsTrue(string.IsNullOrEmpty(_errorModelProvider.Error.MainMessage));
            Assert.IsTrue(string.IsNullOrEmpty(_errorModelProvider.Error.DetailMessage));
            Assert.IsTrue(string.IsNullOrEmpty(_errorModelProvider.Error.LogsPath));
            Assert.IsNull(_errorModelProvider.Error.OriginPage);
        }

        [TestMethod]
        public void Error_Should_RetainValuesWhenSet()
        {
            // Arrange
            var errorModel = new ErrorModel
            {
                MainMessage = "Test error message",
                DetailMessage = "Test error details",
                LogsPath = "C:\\logs\\test.log",
                OriginPage = typeof(object)
            };

            // Act
            _errorModelProvider.Error = errorModel;

            // Assert
            Assert.AreEqual("Test error message", _errorModelProvider.Error.MainMessage);
            Assert.AreEqual("Test error details", _errorModelProvider.Error.DetailMessage);
            Assert.AreEqual("C:\\logs\\test.log", _errorModelProvider.Error.LogsPath);
            Assert.AreEqual(typeof(object), _errorModelProvider.Error.OriginPage);
        }

        [TestMethod]
        public void Error_WhenSetWithNullValue_Should_ReplaceWithEmptyModel()
        {
            // Act
            _errorModelProvider.Error = null;

            // Assert
            Assert.IsNotNull(_errorModelProvider.Error);
            Assert.IsTrue(string.IsNullOrEmpty(_errorModelProvider.Error.MainMessage));
        }

        [TestMethod]
        public void ErrorChanged_Should_FireWhenErrorIsUpdated()
        {
            // Arrange
            bool eventFired = false;
            _errorModelProvider.PropertyChanged += (sender, args) => { eventFired = true; };

            // Act
            _errorModelProvider.Error = new ErrorModel { MainMessage = "New error" };

            // Assert
            Assert.IsTrue(eventFired);
        }

        [TestMethod]
        public void ErrorChanged_Should_NotFireWhenSameErrorIsSetAgain()
        {
            // Arrange
            var error = new ErrorModel { MainMessage = "Test error" };
            _errorModelProvider.Error = error;

            bool eventFired = false;
            _errorModelProvider.PropertyChanged += (sender, args) => { eventFired = true; };

            // Act
            _errorModelProvider.Error = error; // Same instance

            // Assert
            Assert.IsFalse(eventFired);
        }

        
    }
}