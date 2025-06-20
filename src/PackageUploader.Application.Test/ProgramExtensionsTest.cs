// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Extensions;
using System.CommandLine;

namespace PackageUploader.Application.Test
{
    [TestClass]
    public class ProgramExtensionsTest
    {
        [TestMethod]
        [Description("Tests that AddAliasesToSwitchMappings correctly adds all option aliases")]
        public void AddAliasesToSwitchMappings_AddsAllOptionAliases()
        {
            // Arrange
            var option = new Option<string>(["-t", "--TestOption"], "Test option description");
            var switchMappings = new Dictionary<string, string>();
            var configPath = "TestConfig:TestValue";

            // Act
            option.AddAliasesToSwitchMappings(switchMappings, configPath);

            // Assert
            Assert.AreEqual(2, switchMappings.Count, "Should have added 2 aliases");
            Assert.IsTrue(switchMappings.ContainsKey("-t"), "Short alias should be in dictionary");
            Assert.IsTrue(switchMappings.ContainsKey("--TestOption"), "Long alias should be in dictionary");
            Assert.AreEqual(configPath, switchMappings["-t"], "Short alias should map to config path");
            Assert.AreEqual(configPath, switchMappings["--TestOption"], "Long alias should map to config path");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Option must have at least one alias")]
        [Description("Tests that AddAliasesToSwitchMappings works when no aliases are present")]
        public void AddAliasesToSwitchMappings_WithEmptyAliases_DoesNotAddEntries()
        {
            // Arrange
            // Create option with an empty array of aliases
            var option = new Option<string>([], "Empty aliases option");
            var switchMappings = new Dictionary<string, string>();
            var configPath = "TestConfig:TestValue";

            // Act
            option.AddAliasesToSwitchMappings(switchMappings, configPath);

            // Assert
            Assert.AreEqual(0, switchMappings.Count, "Should not have added any entries");
        }
        
        [TestMethod]
        [Description("Tests that Required extension method correctly sets IsRequired")]
        public void Required_SetsIsRequiredProperty()
        {
            // Arrange
            var option = new Option<string>(["-t", "--TestOption"], "Test option description");

            // Act
            var result = option.Required();

            // Assert
            Assert.IsTrue(result.IsRequired, "Option should be marked as required");
            Assert.AreEqual(option, result, "Should return the same option instance");

            // Test setting to false
            result = option.Required(false);
            Assert.IsFalse(result.IsRequired, "Option should be marked as not required");
        }
    }
}