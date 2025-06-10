using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class BaseViewModelTest
    {
        private TestableBaseViewModel _baseViewModel;

        [TestInitialize]
        public void Initialize()
        {
            _baseViewModel = new TestableBaseViewModel();
        }

        [TestMethod]
        public void OnPropertyChanged_RaisesPropertyChangedEvent()
        {
            // Arrange
            bool eventRaised = false;
            string propertyName = null;

            _baseViewModel.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            _baseViewModel.RaisePropertyChanged("TestProperty");

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", propertyName);
        }

        [TestMethod]
        public void SetProperty_WhenValueChanges_ReturnsTrue_AndRaisesEvent()
        {
            // Arrange
            bool eventRaised = false;
            string propertyName = null;
            string initialValue = "initial";

            _baseViewModel.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            bool result = _baseViewModel.TestSetProperty(ref initialValue, "new value");

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", propertyName);
            Assert.AreEqual("new value", initialValue);
        }

        [TestMethod]
        public void SetProperty_WhenValueDoesNotChange_ReturnsFalse_AndDoesNotRaiseEvent()
        {
            // Arrange
            bool eventRaised = false;
            string value = "same";

            _baseViewModel.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
            };

            // Act
            bool result = _baseViewModel.TestSetProperty(ref value, "same");

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(eventRaised);
            Assert.AreEqual("same", value);
        }

        [TestMethod]
        public void SetProperty_WithNullValues_HandlesComparisonCorrectly()
        {
            // Arrange
            string value = null;
            bool eventRaised = false;

            _baseViewModel.PropertyChanged += (sender, args) => { eventRaised = true; };

            // Act - Set null to null (no change)
            bool result1 = _baseViewModel.TestSetProperty(ref value, null);

            // Assert
            Assert.IsFalse(result1);
            Assert.IsFalse(eventRaised);
            Assert.IsNull(value);

            // Act - Set null to non-null (change)
            eventRaised = false;
            bool result2 = _baseViewModel.TestSetProperty(ref value, "non-null");

            // Assert
            Assert.IsTrue(result2);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("non-null", value);
        }
        /* TODO: Figure out how to actually test this, because you cannot replace a private readonly item
        [TestMethod]
        public void SetPropertyInApplicationPreferences_StoresValue()
        {
            // Arrange
            var mockSettingsDictionary = new Dictionary<string, string>();
            ReplaceDictionaryInBaseViewModel(mockSettingsDictionary);

            // Act
            bool result = _baseViewModel.TestSetPropertyInApplicationPreferences("TestSetting", "TestValue");

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(mockSettingsDictionary.ContainsKey("TestSetting"));
            Assert.AreEqual("TestValue", mockSettingsDictionary["TestSetting"]);
        }

        [TestMethod]
        public void GetPropertyFromApplicationPreferences_ReturnsStoredValue()
        {
            // Arrange
            var mockSettingsDictionary = new Dictionary<string, string>
            {
                { "TestSetting", "TestValue" }
            };
            ReplaceDictionaryInBaseViewModel(mockSettingsDictionary);

            // Act
            string result = _baseViewModel.TestGetPropertyFromApplicationPreferences("TestSetting");

            // Assert
            Assert.AreEqual("TestValue", result);
        }

        [TestMethod]
        public void GetPropertyFromApplicationPreferences_WithMissingKey_ReturnsEmptyString()
        {
            // Arrange
            var mockSettingsDictionary = new Dictionary<string, string>();
            ReplaceDictionaryInBaseViewModel(mockSettingsDictionary);

            // Act
            string result = _baseViewModel.TestGetPropertyFromApplicationPreferences("NonExistentSetting");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        // Helper method to replace the static settings dictionary using reflection
        private void ReplaceDictionaryInBaseViewModel(Dictionary<string, string> newDictionary)
        {
            // Get the private static field
            var field = typeof(BaseViewModel).GetField("_settings",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Replace its value
            field?.SetValue(null, newDictionary);
        }*/

        // Testable subclass that exposes protected methods
        private class TestableBaseViewModel : BaseViewModel
        {
            public void RaisePropertyChanged(string propertyName)
            {
                OnPropertyChanged(propertyName);
            }

            public bool TestSetProperty<T>(ref T field, T value, string propertyName = "TestProperty")
            {
                return SetProperty(ref field, value, propertyName);
            }

            public bool TestSetPropertyInApplicationPreferences(string propertyName, string value)
            {
                return SetPropertyInApplicationPreferences(propertyName, value);
            }

            public string TestGetPropertyFromApplicationPreferences(string fieldName)
            {
                return GetPropertyFromApplicationPreferences(fieldName);
            }
        }
    }
}
