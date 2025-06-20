// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Moq;
using PackageUploader.Application.Test.Extensions;
using PackageUploader.ClientApi;
using System.CommandLine;

namespace PackageUploader.Application.Test
{
    [TestClass]
    public class ParameterHelperTest
    {
        [TestMethod]
        [Description("Verifies that the ConfigFileOption is marked as required")]
        public void ConfigFileOption_IsRequired()
        {
            // Assert
            Assert.IsTrue(ParameterHelper.ConfigFileOption.IsRequired);
        }

        [TestMethod]
        [Description("Verifies that the BuildCommandLine method correctly builds a command line with all the expected commands")]
        public void BuildCommandLine_BuildsCommandLineWithAllCommands()
        {
            // Act
            var commandLineBuilder = ParameterHelper.BuildCommandLine();
            var rootCommand = commandLineBuilder.Command as RootCommand;

            // Assert
            Assert.IsNotNull(rootCommand);

            // Expect 9 children (7 commands and 2 global options)
            Assert.AreEqual(9, rootCommand.Children.Count());
            
            // Verify command names
            var commandNames = rootCommand.Children.OfType<Command>().Select(c => c.Name).ToList();
            Assert.IsTrue(commandNames.Contains("GetProduct"));
            Assert.IsTrue(commandNames.Contains("UploadUwpPackage"));
            Assert.IsTrue(commandNames.Contains("UploadXvcPackage"));
            Assert.IsTrue(commandNames.Contains("RemovePackages"));
            Assert.IsTrue(commandNames.Contains("ImportPackages"));
            Assert.IsTrue(commandNames.Contains("PublishPackages"));
            Assert.IsTrue(commandNames.Contains("GetPackages"));
            
            // Verify global options
            Assert.IsTrue(rootCommand.Options.Contains(ParameterHelper.VerboseOption));
            Assert.IsTrue(rootCommand.Options.Contains(ParameterHelper.LogFileOption));
            Assert.AreEqual("Application that enables game developers to upload Xbox and PC game packages to Partner Center", rootCommand.Description);
        }

        [TestMethod]
        [Description("Verifies default authentication method is AppSecret")]
        public void AuthenticationMethodOption_DefaultIsAppSecret()
        {
            // Assert - Use our custom extension method
            Assert.AreEqual(IngestionExtensions.AuthenticationMethod.AppSecret, 
                ParameterHelper.AuthenticationMethodOption.GetDefaultValue());
        }
    }
}