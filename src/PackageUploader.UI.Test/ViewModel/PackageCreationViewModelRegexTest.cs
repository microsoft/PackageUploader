using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.ViewModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public class PackageCreationViewModelRegexTest
    {
        private MethodInfo _xvcPackagePathRegexMethod;
        private MethodInfo _msixvcPackagePathRegexMethod;
        private MethodInfo _encryptionProgressRegexMethod;

        [TestInitialize]
        public void Setup()
        {
            // Get the regex methods using reflection since they're private static methods
            _xvcPackagePathRegexMethod = typeof(PackageCreationViewModel)
                .GetMethod("XvcPackagePathRegex", BindingFlags.NonPublic | BindingFlags.Static);

            _msixvcPackagePathRegexMethod = typeof(PackageCreationViewModel)
                .GetMethod("MsixvcPackagePathRegex", BindingFlags.NonPublic | BindingFlags.Static);

            _encryptionProgressRegexMethod = typeof(PackageCreationViewModel)
                .GetMethod("EncryptionProgressRegex", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [TestMethod]
        public void XvcPackagePathRegex_MatchesValidXvcPath()
        {
            // Arrange
            var regex = (Regex)_xvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Some output text\r\nSuccessfully created package 'C:\\path\\to\\game.xvc'\r\nMore output text";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("C:\\path\\to\\game.xvc", match.Groups["PackagePath"].Value);
        }

        [TestMethod]
        public void XvcPackagePathRegex_DoesNotMatchInvalidPath()
        {
            // Arrange
            var regex = (Regex)_xvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Successfully created file 'C:\\path\\to\\game.xvc'";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsFalse(match.Success);
        }

        [TestMethod]
        public void XvcPackagePathRegex_MatchesPathWithSpecialChars()
        {
            // Arrange
            var regex = (Regex)_xvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Successfully created package 'C:\\path with spaces\\My Game (v1.0).xvc'";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("C:\\path with spaces\\My Game (v1.0).xvc", match.Groups["PackagePath"].Value);
        }

        [TestMethod]
        public void MsixvcPackagePathRegex_MatchesValidMsixvcPath()
        {
            // Arrange
            var regex = (Regex)_msixvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Some output text\r\nSuccessfully created package 'C:\\path\\to\\game.msixvc'\r\nMore output text";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("C:\\path\\to\\game.msixvc", match.Groups["PackagePath"].Value);
        }

        [TestMethod]
        public void MsixvcPackagePathRegex_DoesNotMatchInvalidPath()
        {
            // Arrange
            var regex = (Regex)_msixvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Successfully created file 'C:\\path\\to\\game.msixvc'";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsFalse(match.Success);
        }

        [TestMethod]
        public void MsixvcPackagePathRegex_MatchesPathWithSpecialChars()
        {
            // Arrange
            var regex = (Regex)_msixvcPackagePathRegexMethod.Invoke(null, null);
            string input = "Successfully created package 'C:\\path with spaces\\My Game (v1.0).msixvc'";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("C:\\path with spaces\\My Game (v1.0).msixvc", match.Groups["PackagePath"].Value);
        }

        [TestMethod]
        public void EncryptionProgressRegex_MatchesValidPercentage()
        {
            // Arrange
            var regex = (Regex)_encryptionProgressRegexMethod.Invoke(null, null);
            string input = "Some output text\r\nEncrypted 50 %\r\nMore output text";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("50", match.Groups[1].Value);
        }

        [TestMethod]
        public void EncryptionProgressRegex_MatchesZeroPercent()
        {
            // Arrange
            var regex = (Regex)_encryptionProgressRegexMethod.Invoke(null, null);
            string input = "Encrypted 0 %";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("0", match.Groups[1].Value);
        }

        [TestMethod]
        public void EncryptionProgressRegex_Matches100Percent()
        {
            // Arrange
            var regex = (Regex)_encryptionProgressRegexMethod.Invoke(null, null);
            string input = "Encrypted 100 %";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsTrue(match.Success);
            Assert.AreEqual("100", match.Groups[1].Value);
        }

        [TestMethod]
        public void EncryptionProgressRegex_DoesNotMatchInvalidFormat()
        {
            // Arrange
            var regex = (Regex)_encryptionProgressRegexMethod.Invoke(null, null);
            string input = "Encrypting 50 %";

            // Act
            var match = regex.Match(input);

            // Assert
            Assert.IsFalse(match.Success);
        }
    }
}