// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi;
using System.CommandLine;

namespace PackageUploader.Application.Test
{
    [TestClass]
    public class CommandLineHelperTest
    {
        [TestMethod]
        [Description("Verifies that the ConfigFileOption is marked as required")]
        public void ConfigFileOption_IsRequired()
        {
            // Assert
            Assert.IsTrue(CommandLineHelper.ConfigFileOption.Required);
        }

        [TestMethod]
        [Description("Verifies that the BuildCommandLine method correctly builds a command line with all the expected commands")]
        public void BuildCommandLine_BuildsCommandLineWithAllCommands()
        {
            // Act
            var commandLineBuilder = CommandLineHelper.BuildRootCommand();
            var rootCommand = commandLineBuilder;

            // Assert
            Assert.IsNotNull(rootCommand);

            // Verify command names (7 commands)
            var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
            Assert.IsTrue(commandNames.Contains("GetProduct"));
            Assert.IsTrue(commandNames.Contains("UploadUwpPackage"));
            Assert.IsTrue(commandNames.Contains("UploadXvcPackage"));
            Assert.IsTrue(commandNames.Contains("RemovePackages"));
            Assert.IsTrue(commandNames.Contains("ImportPackages"));
            Assert.IsTrue(commandNames.Contains("PublishPackages"));
            Assert.IsTrue(commandNames.Contains("GetPackages"));
            
            // Verify global options
            Assert.IsTrue(rootCommand.Options.Contains(CommandLineHelper.VerboseOption));
            Assert.IsTrue(rootCommand.Options.Contains(CommandLineHelper.LogFileOption));
            Assert.AreEqual("Application that enables game developers to upload Xbox and PC game packages to Partner Center", rootCommand.Description);
        }

        [TestMethod]
        [Description("Verifies default authentication method is AppSecret")]
        public void AuthenticationMethodOption_DefaultIsAppSecret()
        {
            // Assert - Use our custom extension method
            Assert.AreEqual(IngestionExtensions.AuthenticationMethod.AppSecret, 
                CommandLineHelper.AuthenticationMethodOption.GetDefaultValue());
        }

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