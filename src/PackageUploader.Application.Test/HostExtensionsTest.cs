// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Extensions;
using System.CommandLine;

namespace PackageUploader.Application.Test
{
    [TestClass]
    public class HostExtensionsTest
    {
        [TestMethod]
        [Description("Tests that AddAliasesToSwitchMappings correctly adds all option aliases")]
        public void AddAliasesToSwitchMappings_AddsAllOptionAliases()
        {
            // Arrange
            var option = new Option<string>("--TestOption") { Description = "Test option description", Aliases = { "-t" } };
            var switchMappings = new Dictionary<string, string>();
            var configPath = "TestConfig:TestValue";

            // Act
            option.AddAliasesToSwitchMappings(switchMappings, configPath);

            // Assert
            Assert.AreEqual(2, switchMappings.Count, "Should have added name and alias");
            Assert.IsTrue(switchMappings.ContainsKey("-t"), "Short alias should be in dictionary");
            Assert.IsTrue(switchMappings.ContainsKey("--TestOption"), "Name should be in dictionary");
            Assert.AreEqual(configPath, switchMappings["-t"], "Short alias should map to config path");
            Assert.AreEqual(configPath, switchMappings["--TestOption"], "Long alias should map to config path");
        }

        [TestMethod]
        [Description("Tests that AddAliasesToSwitchMappings works when no aliases are present")]
        public void AddAliasesToSwitchMappings_WithEmptyAliases_DoesNotAddEntries()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => new Option<string>(""), "Option must have a valid name");
        }
    }
}
