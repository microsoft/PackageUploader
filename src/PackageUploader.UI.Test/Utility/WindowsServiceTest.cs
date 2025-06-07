// Almost entirely written by CoPilot
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Utility;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PackageUploader.UI.Test.Utility
{
    [TestClass]
    public class WindowServiceTest
    {
        private Mock<ContentControl> _mockContentControl;
        private Mock<IServiceProvider> _mockServiceProvider;
        private WindowService _windowService;

        [TestInitialize]
        public void Setup()
        {
            _mockContentControl = new Mock<ContentControl>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _windowService = new WindowService(_mockContentControl.Object, _mockServiceProvider.Object);

            // Ensure Application.Current is initialized without directly assigning to it  
            if (System.Windows.Application.Current == null)
            {
                new Application(); // Create a new Application instance  
            }
        }

        #region Constructor Tests

        [WpfTestMethod]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act - constructor already called in Setup

            // Assert - no exception thrown
            Assert.IsNotNull(_windowService);
        }

        [WpfTestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullContentControl_ThrowsArgumentNullException()
        {
            // Act
            _ = new WindowService(null, _mockServiceProvider.Object);
        }

        [WpfTestMethod]
        [ExpectedException(typeof(ArgumentNullException))] // That's pretty cool
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act
            _ = new WindowService(_mockContentControl.Object, null);
        }

        #endregion

        #region NavigateTo<T> Tests

        [WpfTestMethod]
        public void NavigateToGeneric_WithValidType_SetsContentControl()
        {
            // Act
            _windowService.NavigateTo<TestUserControl>();

            // Assert
            Assert.IsNotNull(_mockContentControl.Object.Content);
            Assert.IsInstanceOfType(_mockContentControl.Object.Content, typeof(TestUserControl));
        }

        #endregion

        #region NavigateTo(Type) Tests

        [WpfTestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NavigateToType_WithNonUIElementType_ThrowsArgumentException()
        {
            // Act
            _windowService.NavigateTo(typeof(NonUIElementClass));
        }

        [WpfTestMethod]
        public void NavigateToType_ResolvesFromServiceProvider_SetsContentControl()
        {
            // Arrange
            var testControl = new TestUserControl();
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TestUserControl)))
                .Returns(testControl);

            // Act
            _windowService.NavigateTo(typeof(TestUserControl));

            // Assert
            Assert.AreEqual(testControl, _mockContentControl.Object.Content);
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(TestUserControl)), Times.Once);
        }

        [WpfTestMethod]
        public void NavigateToType_WhenServiceProviderReturnsNull_FallsBackToActivator()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TestUserControl)))
                .Returns(null);

            // Act
            _windowService.NavigateTo(typeof(TestUserControl));

            // Assert
            Assert.IsNotNull(_mockContentControl.Object.Content);
            Assert.IsInstanceOfType(_mockContentControl.Object.Content, typeof(TestUserControl));
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(TestUserControl)), Times.Once);
        }

        [WpfTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NavigateToType_WhenBothResolutionMethodsFail_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(NoParameterlessConstructorControl)))
                .Returns(null);

            // Act
            _windowService.NavigateTo(typeof(NoParameterlessConstructorControl));
        }

        #endregion

        #region ShowDialog Tests

        [WpfTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShowDialog_WithNonWindowType_ThrowsInvalidOperationException()
        {
            // Act
            _windowService.ShowDialog<TestUserControl>();
        }

        /*[WpfTestMethod]
        public void ShowDialog_ResolvesFromServiceProvider()
        {
            // Arrange
            var mockWindow = new Mock<TestWindow>();
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TestWindow)))
                .Returns(mockWindow.Object);

            // Setup current application
            var currentApp = new Mock<Application>();
            var mainWindow = new Mock<Window>();
            System.Windows.Application.Current.MainWindow = mainWindow.Object;

            //Window originalMainWindow = Application.Current.MainWindow;

            // Act
            _windowService.ShowDialog<TestWindow>();

            // Assert
            mockWindow.VerifySet(w => w.Owner = mainWindow.Object, Times.Once);
            mockWindow.Verify(w => w.ShowDialog(), Times.Once);

        }

        [WpfTestMethod]
        public void ShowDialog_WhenServiceProviderReturnsNull_FallsBackToActivator()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TestWindow)))
                .Returns(null);

            // Setup current application
            var mockWindow = new Mock<TestWindow>();
            //var activatorMock = new Mock<Activator>();

            // This test is challenging because we can't easily mock static Activator.CreateInstance
            // In a real test, we would need to use a wrapper or dependency injection for Activator

            // For this test, we'll assume the behavior works if no exception is thrown
            // A more comprehensive test would need a different approach

            try
            {
                // Mock Application.Current
                var currentApp = new Mock<Application>();
                var mainWindow = new Mock<Window>();

                // Hack to set Application.Current
                typeof(Application).GetProperty("Current", System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static)?.SetValue(null, currentApp.Object);

                currentApp.SetupGet(a => a.MainWindow).Returns(mainWindow.Object);

                // We can't execute the real test due to the static Activator.CreateInstance call
                // Skip actual execution for this test
                // _windowService.ShowDialog<TestWindow>();
            }
            catch (Exception)
            {
                // We expect this might throw due to our limited mocking capabilities
            }
            finally
            {
                // Clean up any Application.Current modification
                //typeof(Application).GetProperty("Current", System.Reflection.BindingFlags.Public |
                //    System.Reflection.BindingFlags.Static)?.SetValue(null, null);
            }
        }*/

        [WpfTestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShowDialog_WhenBothResolutionMethodsFail_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(NoParameterlessWindow)))
                .Returns(null);

            // Act
            _windowService.ShowDialog<NoParameterlessWindow>();
        }

        #endregion
    }

    // Test classes
    public class TestUserControl : UserControl { }

    public class NoParameterlessConstructorControl : UserControl
    {
        public NoParameterlessConstructorControl(string parameter)
        {
            // This constructor requires a parameter
        }
    }

    public class NonUIElementClass { }

    public class TestWindow : Window { }

    public class NoParameterlessWindow : Window
    {
        public NoParameterlessWindow(string parameter)
        {
            // This constructor requires a parameter
        }
    }
}
