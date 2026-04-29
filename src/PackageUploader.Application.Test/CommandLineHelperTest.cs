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
            Assert.Contains("GetProduct", commandNames);
            Assert.Contains("UploadUwpPackage", commandNames);
            Assert.Contains("UploadXvcPackage", commandNames);
            Assert.Contains("RemovePackages", commandNames);
            Assert.Contains("ImportPackages", commandNames);
            Assert.Contains("PublishPackages", commandNames);
            Assert.Contains("GetPackages", commandNames);
            
            // Verify global options
            Assert.Contains(CommandLineHelper.VerboseOption, rootCommand.Options);
            Assert.Contains(CommandLineHelper.LogFileOption, rootCommand.Options);
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
    }
}