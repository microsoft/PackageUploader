// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PackageUploader.Application.Extensions;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Config;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider.Models;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace PackageUploader.Application.Test
{
    [TestClass]
    public class ParameterHelperIntegrationTest
    {
        private string? _tempJsonFilePath;
        private IConfigurationRoot? _configuration;
        private Parser? _parser;

        [TestInitialize]
        public void Initialize()
        {
            // Setup a temporary config file
            _tempJsonFilePath = Path.GetTempFileName();
            
            // Create a test JSON config
            var configJson = new
            {
                OperationName = "GetProduct",
                ProductId = "test-product-id",
                BigId = "FakeBigId",

                // Some fake authentication info for testing
                AadAuth = new
                {
                    TenantId = "test-tenant-id",
                    ClientId = "test-client-id",
                }
            };

            File.WriteAllText(_tempJsonFilePath, JsonSerializer.Serialize(configJson));

            // Set up parser
            _parser = ParameterHelper.BuildCommandLine().Build();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up the temporary file
            if (_tempJsonFilePath != null && File.Exists(_tempJsonFilePath))
            {
                File.Delete(_tempJsonFilePath);
            }
        }

        [TestMethod]
        [Description("Test that config file is required")]
        public void Parse_WithoutConfigFile_Fails()
        {
            // Arrange
            var args = new[] { "GetProduct" };

            // Act
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);

            // Assert
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("Option '-c' is required."));
        }

        [TestMethod]
        [Description("Test that ProductId command line parameter overrides config file value")]
        public void CommandLineParameter_OverridesConfigValue()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            var args = new[] { "GetProduct", "--ConfigFile", _tempJsonFilePath!, "--ProductId", "override-product-id" };
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);
            
            // Mock the invocation context to get the option value
            InvocationContext invocationContext = new(result);
            var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
            var authMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

            // Act
            ParameterHelper.ConfigureParameters(configFile, authMethod, configBuilder, args);
            _configuration = configBuilder.Build();

            // Assert
            Assert.AreEqual("override-product-id", _configuration["ProductId"]);
        }

        [TestMethod]
        [Description("Test that AppSecret authentication method requires ClientSecret and throws exception when missing")]
        public void AppSecret_RequiresClientSecret()
        {
            // Create direct configuration for the test with required properties except ClientSecret
            Dictionary<string, string?>? configValues = new()
            {
                // Need to include TenantId and ClientId to ensure those validations pass
                [$"{AadAuthInfo.ConfigName}:{nameof(AadAuthInfo.TenantId)}"] = "test-tenant-id",
                [$"{AadAuthInfo.ConfigName}:{nameof(AadAuthInfo.ClientId)}"] = "test-client-id",
                // Intentionally omitting ClientSecret
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Setup the services
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddOptions<AccessTokenProviderConfig>().Bind(configuration);
            services.AddOptions<AzureApplicationSecretAuthInfo>().Bind(configuration.GetSection(AadAuthInfo.ConfigName));
            var serviceProvider = services.BuildServiceProvider();

            // Verify the configuration is set correctly for TenantId and ClientId
            var authInfoOptions = serviceProvider.GetService<IOptions<AzureApplicationSecretAuthInfo>>();
            Assert.IsNotNull(authInfoOptions);
            var authInfo = authInfoOptions.Value;
            Assert.IsNotNull(authInfo);
            Assert.AreEqual("test-tenant-id", authInfo.TenantId);
            Assert.AreEqual("test-client-id", authInfo.ClientId);
            // ClientSecret should be null or empty
            Assert.IsTrue(string.IsNullOrEmpty(authInfo.ClientSecret));
            
            // Act and Assert - verify exception is thrown due to missing ClientSecret
            Assert.ThrowsException<ArgumentException>(() => 
            {
                var tokenProvider = new AzureApplicationSecretAccessTokenProvider(
                    serviceProvider.GetRequiredService<IOptions<AccessTokenProviderConfig>>(),
                    serviceProvider.GetRequiredService<IOptions<AzureApplicationSecretAuthInfo>>(),
                    serviceProvider.GetRequiredService<ILogger<AzureApplicationSecretAccessTokenProvider>>()
                );
            }, "Should throw ArgumentException when ClientSecret is missing");

            // Now let's add the client secret and try again
            configValues = new()
            {
                [$"{AadAuthInfo.ConfigName}:{nameof(AadAuthInfo.TenantId)}"] = "test-tenant-id",
                [$"{AadAuthInfo.ConfigName}:{nameof(AadAuthInfo.ClientId)}"] = "test-client-id",
                [$"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}"] = "test-client-secret"
            };

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            // Setup the services with the updated configuration
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddOptions<AccessTokenProviderConfig>().Bind(configuration);
            services.AddOptions<AzureApplicationSecretAuthInfo>().Bind(configuration.GetSection(AadAuthInfo.ConfigName));
            serviceProvider = services.BuildServiceProvider();

            // Verify client secret is now set
            authInfoOptions = serviceProvider.GetService<IOptions<AzureApplicationSecretAuthInfo>>();
            Assert.IsNotNull(authInfoOptions);
            authInfo = authInfoOptions.Value;
            Assert.IsNotNull(authInfo);
            Assert.AreEqual("test-client-secret", authInfo.ClientSecret);

            // This should not throw now
            var validTokenProvider = new AzureApplicationSecretAccessTokenProvider(
                serviceProvider.GetRequiredService<IOptions<AccessTokenProviderConfig>>(),
                serviceProvider.GetRequiredService<IOptions<AzureApplicationSecretAuthInfo>>(),
                serviceProvider.GetRequiredService<ILogger<AzureApplicationSecretAccessTokenProvider>>()
            );

            Assert.IsNotNull(validTokenProvider, "Token provider should be created successfully with ClientSecret");
        }

        [TestMethod]
        [Description("Test that Browser authentication method accepts TenantId")]
        public void Browser_AcceptsTenantId()
        {
            // Arrange
            var args = new[] { 
                "GetProduct", 
                "--ConfigFile", _tempJsonFilePath!, 
                "--Authentication", "Browser",
                "--TenantId", "my-tenant-id" 
            };
            var configBuilder = new ConfigurationBuilder();
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);

            // Mock the invocation context to get the option value
            InvocationContext invocationContext = new(result);
            var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
            var authMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

            // Act
            ParameterHelper.ConfigureParameters(configFile, authMethod, configBuilder, args);
            _configuration = configBuilder.Build();

            // Assert
            Assert.AreEqual("my-tenant-id", _configuration[$"{BrowserAuthInfo.ConfigName}:{nameof(BrowserAuthInfo.TenantId)}"]);
        }

        [TestMethod]
        [Description("Test that CacheableBrowser authentication method accepts TenantId")]
        public void CacheableBrowser_AcceptsTenantId()
        {
            // Arrange
            var args = new[] { 
                "GetProduct", 
                "--ConfigFile", _tempJsonFilePath!, 
                "--Authentication", "CacheableBrowser",
                "--TenantId", "my-cached-tenant-id" 
            };
            var configBuilder = new ConfigurationBuilder();
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);

            // Mock the invocation context to get the option value
            InvocationContext invocationContext = new(result);
            var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
            var authMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

            // Act
            ParameterHelper.ConfigureParameters(configFile, authMethod, configBuilder, args);
            _configuration = configBuilder.Build();

            // Assert
            Assert.AreEqual("my-cached-tenant-id", _configuration[$"{BrowserAuthInfo.ConfigName}:{nameof(BrowserAuthInfo.TenantId)}"]);
        }

        [TestMethod]
        [Description("Test that config file values are extracted correctly")]
        public void ConfigFileValues_AreExtractedCorrectly()
        {
            // Arrange
            var complexConfig = new
            {
                OperationName = "GetProduct",
                ProductId = "product-from-config",
                BigId = "",
                AadAuth = new
                {
                    ClientSecret = "secret-from-config"
                }
            };

            Assert.IsNotNull(_tempJsonFilePath);
            File.WriteAllText(_tempJsonFilePath, JsonSerializer.Serialize(complexConfig));

            var args = new[] { 
                "GetProduct", 
                "--ConfigFile", _tempJsonFilePath, 
                "--Authentication", "AppSecret"
            };
            
            var configBuilder = new ConfigurationBuilder();
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);

            // Mock the invocation context to get the option value
            InvocationContext invocationContext = new(result);
            var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
            var authMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

            // Act
            ParameterHelper.ConfigureParameters(configFile, authMethod, configBuilder, args);
            _configuration = configBuilder.Build();

            // Assert
            Assert.AreEqual("product-from-config", _configuration["ProductId"]);
            Assert.AreEqual("secret-from-config", _configuration["AadAuth:ClientSecret"]);
        }

        [TestMethod]
        [Description("Test all command line options are bound correctly")]
        public void AllCommandLineOptions_AreBoundCorrectly()
        {
            // Arrange
            var args = new[] { 
                "GetProduct", 
                "--ConfigFile", _tempJsonFilePath!,
                "--ProductId", "product-from-cmd",
                "--BigId", "big-id-from-cmd",
                "--BranchFriendlyName", "branch-from-cmd",
                "--FlightName", "flight-from-cmd",
                "--MarketGroupName", "market-from-cmd",
                "--DestinationSandboxName", "sandbox-from-cmd",
                "--Authentication", "AppSecret",
                "--ClientSecret", "secret-from-cmd",
                "--Verbose"
            };
            
            var configBuilder = new ConfigurationBuilder();
            Assert.IsNotNull(_parser);
            var result = _parser.Parse(args);

            // Mock the invocation context to get the option value
            InvocationContext invocationContext = new(result);
            var configFile = invocationContext.GetOptionValue(ParameterHelper.ConfigFileOption);
            var authMethod = invocationContext.GetOptionValue(ParameterHelper.AuthenticationMethodOption);

            // Act
            ParameterHelper.ConfigureParameters(configFile, authMethod, configBuilder, args);
            _configuration = configBuilder.Build();

            // Assert
            Assert.AreEqual("product-from-cmd", _configuration["ProductId"]);
            Assert.AreEqual("big-id-from-cmd", _configuration["BigId"]);
            Assert.AreEqual("branch-from-cmd", _configuration["BranchFriendlyName"]);
            Assert.AreEqual("flight-from-cmd", _configuration["FlightName"]);
            Assert.AreEqual("market-from-cmd", _configuration["MarketGroupName"]);
            Assert.AreEqual("sandbox-from-cmd", _configuration["DestinationSandboxName"]);
            Assert.AreEqual("secret-from-cmd", _configuration[$"{AadAuthInfo.ConfigName}:{nameof(AzureApplicationSecretAuthInfo.ClientSecret)}"]);
        }
    }
}