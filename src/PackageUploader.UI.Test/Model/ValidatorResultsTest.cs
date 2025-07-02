using Moq;
using PackageUploader.UI.Model;
using System.Text;
using System.Xml;

namespace PackageUploader.UI.Test.Model;

[TestClass]
public class ValidatorResultsTest
{
    private ValidatorResults _validator;

    [TestInitialize]
    public void Setup()
    {
        _validator = new ValidatorResults();
    }

    [TestMethod]
    public void TestGetsSets()
    {
        var productId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var buildId = Guid.NewGuid();
        var type = "type";
        var name = "name";
        _validator.ProductId = productId;
        _validator.ContentId = contentId;
        _validator.BuildId = buildId;
        _validator.Type = type;
        _validator.Name = name;
        _validator.TotalErrors = 1;
        _validator.TotalWarnings = 2;
        _validator.Succeeded = true;
        Assert.AreEqual(productId, _validator.ProductId);
        Assert.AreEqual(contentId, _validator.ContentId);
        Assert.AreEqual(buildId, _validator.BuildId);
        Assert.AreEqual(type, _validator.Type);
        Assert.AreEqual(name, _validator.Name);
        Assert.AreEqual(1, _validator.TotalErrors);
        Assert.AreEqual(2, _validator.TotalWarnings);
        Assert.IsTrue( _validator.Succeeded);
    }

    [TestMethod]
    public void TestReset()
    {
        _validator.ProductId = Guid.NewGuid();
        _validator.ContentId = Guid.NewGuid();
        _validator.BuildId = Guid.NewGuid();
        _validator.Type = "type";
        _validator.Name = "name";
        _validator.TotalErrors = 1;
        _validator.TotalWarnings = 2;
        _validator.Succeeded = true;
        _validator.Reset();
        Assert.AreEqual(Guid.Empty, _validator.ProductId);
        Assert.AreEqual(Guid.Empty, _validator.ContentId);
        Assert.AreEqual(Guid.Empty, _validator.BuildId);
        Assert.AreEqual(string.Empty, _validator.Type);
        Assert.AreEqual(string.Empty, _validator.Name);
        Assert.AreEqual(-1, _validator.TotalErrors);
        Assert.AreEqual(-1, _validator.TotalWarnings);
        Assert.IsFalse(_validator.Succeeded);
    }

    [TestMethod]
    public void TestValidatorTestResult()
    {
        var testResult = new ValidatorTestResult();

        // Set Up
        var Id= "hello";
        var Type = ValidatorTestResultType.Failure;
        var Message = "message";
        var toString = "hello: message";

        // Act
        testResult.Id = Id;
        testResult.Type = Type;
        testResult.Message = Message;

        // Assert
        Assert.AreEqual(Id, testResult.Id);
        Assert.AreEqual(Type, testResult.Type);
        Assert.AreEqual(Message, testResult.Message);

        // Act and Assert for ToString
        Assert.AreEqual(toString, testResult.ToString());
    }

    [TestMethod]
    public void TestValidatorComponentGetSet()
    {
        var component = new ValidatorComponent();
        // Set Up
        var componentString = "helloWorld";
        var testResult = new ValidatorTestResult();

        // Act
        component.Component = componentString;
        component.Items.Add(testResult);

        // Assert
        Assert.AreEqual(componentString, component.Component);
        Assert.AreEqual(1, component.Items.Count);
        Assert.AreEqual(testResult, component.Items[0]);
    }

    [TestMethod]
    public void TestValidatorComponentParse()
    {
        var component = new ValidatorComponent();

        // it's a base class, so ParseResultTag shouldn't do anything
        Mock<XmlReader> reader = new Mock<XmlReader>();

        reader.Setup(r => r.Read()).Returns(true);

        component.ParseResultTag(reader.Object);

        reader.Verify(r => r.Read(), Times.Never);
    }

    [TestMethod]
    public void TestValidatorComponentToolsCheckGetCheck()
    {
        var component = new ValidatorComponentToolsCheck();

        var makePkgVersion = "1.2.3.4";
        var packagingServicesVersion = "helloWorld";
        var xsapiVersion = "1.0.0";
        var xcrdapiVersion = "1.0.0";
        var xcitreeVersion = "1.0.0";
        var windowsVersion = "1.0.0";
        var windows10Sdk = "1.0.0";
        var grtsVersion = "1.0.0";
        var xcapiVersion = "1.0.0";

        // Act
        component.MakePkg_Version = makePkgVersion;
        component.PackagingServices_Version = packagingServicesVersion;
        component.XSAPI_Version = xsapiVersion;
        component.XCRDAPI_Version = xcrdapiVersion;
        component.XCITREE_Version = xcitreeVersion;
        component.Windows_Version = windowsVersion;
        component.Windows_10_SDK = windows10Sdk;
        component.GRTS_Version = grtsVersion;
        component.XCAPI_Version = xcapiVersion;

        // Assert
        
        Assert.AreEqual(makePkgVersion, component.MakePkg_Version);
        Assert.AreEqual(packagingServicesVersion, component.PackagingServices_Version);
        Assert.AreEqual(xsapiVersion, component.XSAPI_Version);
        Assert.AreEqual(xcrdapiVersion, component.XCRDAPI_Version);
        Assert.AreEqual(xcitreeVersion, component.XCITREE_Version);
        Assert.AreEqual(windowsVersion, component.Windows_Version);
        Assert.AreEqual(windows10Sdk, component.Windows_10_SDK);
        Assert.AreEqual(grtsVersion, component.GRTS_Version);
        Assert.AreEqual(xcapiVersion, component.XCAPI_Version);
    }

    [TestMethod]
    public void TestValidatorComponentToolsCheckParse()
    {
        var component = new ValidatorComponentToolsCheck();
        // Set Up
        var makePkgVersion = "1.2.3.4";
        var packagingServicesVersion = "helloWorld";
        var xsapiVersion = "1.0.0";
        var xcrdapiVersion = "1.0.0";
        var xcitreeVersion = "1.0.0";
        var windowsVersion = "1.0.0";
        var windows10Sdk = "1.0.0";
        var grtsVersion = "1.0.0";
        var xcapiVersion = "1.0.0";

        var testXml = $@"<testresult>
                            <component>Tools Check</component>
                            <MakePkg_version>{makePkgVersion}</MakePkg_version>
                            <PackagingServices_version>{packagingServicesVersion}</PackagingServices_version>
                            <XSAPI_version>{xsapiVersion}</XSAPI_version>
                            <XCRDAPI_version>{xcrdapiVersion}</XCRDAPI_version>
                            <XCITREE_version>{xcitreeVersion}</XCITREE_version>
                            <Windows_version>{windowsVersion}</Windows_version>
                            <Windows_10_SDK>{windows10Sdk}</Windows_10_SDK>
                            <GRTS_version>{grtsVersion}</GRTS_version>
                            <XCAPI_version>{xcapiVersion}</XCAPI_version>
                        </testresult>";
        var reader = XmlReader.Create(new StringReader(testXml));

        // Act
        while (reader.Read())
        {
            component.ParseResultTag(reader);
        }

        // Assert
        Assert.AreEqual(makePkgVersion, component.MakePkg_Version);
        Assert.AreEqual(packagingServicesVersion, component.PackagingServices_Version);
        Assert.AreEqual(xsapiVersion, component.XSAPI_Version);
        Assert.AreEqual(xcrdapiVersion, component.XCRDAPI_Version);
        Assert.AreEqual(xcitreeVersion, component.XCITREE_Version);
        Assert.AreEqual(windowsVersion, component.Windows_Version);
        Assert.AreEqual(windows10Sdk, component.Windows_10_SDK);
        Assert.AreEqual(grtsVersion, component.GRTS_Version);
        Assert.AreEqual(xcapiVersion, component.XCAPI_Version);

        // Act and Assert Override test
        component = new ValidatorComponentToolsCheck();

        ValidatorComponent c = component;
        reader = XmlReader.Create(new StringReader(testXml));
        while (reader.Read())
        {
            c.ParseResultTag(reader);
        }

        Assert.AreEqual(makePkgVersion, component.MakePkg_Version);
        Assert.AreEqual(packagingServicesVersion, component.PackagingServices_Version);
        Assert.AreEqual(xsapiVersion, component.XSAPI_Version);
        Assert.AreEqual(xcrdapiVersion, component.XCRDAPI_Version);
        Assert.AreEqual(xcitreeVersion, component.XCITREE_Version);
        Assert.AreEqual(windowsVersion, component.Windows_Version);
        Assert.AreEqual(windows10Sdk, component.Windows_10_SDK);
        Assert.AreEqual(grtsVersion, component.GRTS_Version);
        Assert.AreEqual(xcapiVersion, component.XCAPI_Version);
    }

    [ExpectedException(typeof(InvalidDataException))]
    [TestMethod]
    public void TestValidatorResultsParseFailNoSuchFile()
    {
        _validator.Parse("helloWorld");
    }
    
    [ExpectedException(typeof(InvalidDataException))]
    [TestMethod]
    public void TestValidatorResultsParseFailNull()
    {
        _validator.Parse(null);
    }

    [ExpectedException(typeof(InvalidDataException))]
    [TestMethod]
    public void TestValidatorResultsParseFailEmptyString()
    {
        _validator.Parse("");
    }

    [TestMethod]
    public void TestFullParsing()
    {
        var productId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var buildId = Guid.NewGuid();
        var type = "type";
        var name = "name";
        var totalFailures = 2;
        var totalWarnings = 1;

        // component 1
        var component1Name = "helloWorldComponent1";
        var validatorTestResult1Id = "helloWorldError1";
        var validatorTestResult1Type = ValidatorTestResultType.Failure;
        var validatorTestResult1Message = "helloWorldError1Message";

        // component 2
        var component2Name = "helloWorldComponent2";
        var validatorTestResult2Id = "helloWorldError2";
        var validatorTestResult2Type = ValidatorTestResultType.Failure;
        var validatorTestResult2Message = "helloWorldError2Message";
        
        var validatorTestResult3Id = "helloWorldWarning1";
        var validatorTestResult3Type = ValidatorTestResultType.Warning;
        var validatorTestResult3Message = "helloWorldWarning1Message";

        // component 3
        var component3Name = "helloWorldComponent3";
        var validatorTestResult4Id = "helloWorldInfo3";
        var validatorTestResult4Type = ValidatorTestResultType.Info;
        var validatorTestResult4Message = "helloWorldInfo3Message";

        // ToolsCheck component
        var makePkgVersion = "1.2.3.4";
        var packagingServicesVersion = "helloWorld";
        var xsapiVersion = "1.0.0";
        var xcrdapiVersion = "1.0.0";
        var xcitreeVersion = "1.0.0";
        var windowsVersion = "1.0.0";
        var windows10Sdk = "1.0.0";
        var grtsVersion = "1.0.0";
        var xcapiVersion = "1.0.0";

        var xml = $@"<XboxOneSubmissionValidator>
                        <project>
                            <ProductId>{productId}</ProductId>
                            <ContentId>{contentId}</ContentId>
                            <BuildId>{buildId}</BuildId>
                            <Type>{type}</Type>
                            <Name>{name}</Name>
                        </project>
                        <ValidationSummary>
                            <TotalFailures>{totalFailures}</TotalFailures>
                            <TotalWarnings>{totalWarnings}</TotalWarnings>
                            <Result>Fail</Result>
                            <WarningsSummary>
                                <Warning>HelloWorldWarning1</Warning>
                            </WarningsSummary>
                            <FailuresSummary>
                                <Failure>HelloWorldError1</Failure>
                                <Failure>HelloWorldError2</Failure>
                            </FailuresSummary>
                        </ValidationSummary>
                        <testresults>
                            <testresult>
                                <component>{component1Name}</component>
                                <failure Id=""{validatorTestResult1Id}"">{validatorTestResult1Message}</failure>
                            </testresult>
                            <testresult>
                                <component>{component2Name}</component>
                                <failure Id=""{validatorTestResult2Id}"">{validatorTestResult2Message}</failure>
                                <warning Id=""{validatorTestResult3Id}"">{validatorTestResult3Message}</warning>
                            </testresult>
                            <testresult>
                                <component>{component3Name}</component>
                                <info Id=""{validatorTestResult4Id}"">{validatorTestResult4Message}</info>
                            </testresult>
                            <testresult>
                                <component>Tools Check</component>
                                <MakePkg_version>{makePkgVersion}</MakePkg_version>
                                <PackagingServices_version>{packagingServicesVersion}</PackagingServices_version>
                                <XSAPI_version>{xsapiVersion}</XSAPI_version>
                                <XCRDAPI_version>{xcrdapiVersion}</XCRDAPI_version>
                                <XCITREE_version>{xcitreeVersion}</XCITREE_version>
                                <Windows_version>{windowsVersion}</Windows_version>
                                <Windows_10_SDK>{windows10Sdk}</Windows_10_SDK>
                                <GRTS_version>{grtsVersion}</GRTS_version>
                                <XCAPI_version>{xcapiVersion}</XCAPI_version>
                            </testresult>
                        </testresults>
                    </XboxOneSubmissionValidator>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, Encoding.UTF8.GetBytes(xml));

        // Act
        _validator.Parse(tempFile);

        // Assert
        Assert.IsFalse(_validator.Succeeded);
        Assert.AreEqual(totalFailures, _validator.TotalErrors);
        Assert.AreEqual(totalWarnings, _validator.TotalWarnings);
        Assert.AreEqual(productId, _validator.ProductId);
        Assert.AreEqual(contentId, _validator.ContentId);
        Assert.AreEqual(buildId, _validator.BuildId);
        Assert.AreEqual(type, _validator.Type);
        Assert.AreEqual(name, _validator.Name);
        Assert.AreEqual(4, _validator.Components.Count);

        // Assume order of components
        Assert.AreEqual(component1Name, _validator.Components[0].Component);
        Assert.AreEqual(1, _validator.Components[0].Items.Count);
        Assert.AreEqual(validatorTestResult1Id, _validator.Components[0].Items[0].Id);
        Assert.AreEqual(validatorTestResult1Type, _validator.Components[0].Items[0].Type);
        Assert.AreEqual(validatorTestResult1Message, _validator.Components[0].Items[0].Message);

        Assert.AreEqual(component2Name, _validator.Components[1].Component);
        Assert.AreEqual(2, _validator.Components[1].Items.Count);
        Assert.AreEqual(validatorTestResult2Id, _validator.Components[1].Items[0].Id);
        Assert.AreEqual(validatorTestResult2Type, _validator.Components[1].Items[0].Type);
        Assert.AreEqual(validatorTestResult2Message, _validator.Components[1].Items[0].Message);
        Assert.AreEqual(validatorTestResult3Id, _validator.Components[1].Items[1].Id);
        Assert.AreEqual(validatorTestResult3Type, _validator.Components[1].Items[1].Type);
        Assert.AreEqual(validatorTestResult3Message, _validator.Components[1].Items[1].Message);

        Assert.AreEqual(component3Name, _validator.Components[2].Component);
        Assert.AreEqual(1, _validator.Components[2].Items.Count);
        Assert.AreEqual(validatorTestResult4Id, _validator.Components[2].Items[0].Id);
        Assert.AreEqual(validatorTestResult4Type, _validator.Components[2].Items[0].Type);
        Assert.AreEqual(validatorTestResult4Message, _validator.Components[2].Items[0].Message);
        
        Assert.AreEqual("Tools Check", _validator.Components[3].Component);
        Assert.AreEqual(makePkgVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).MakePkg_Version);
        Assert.AreEqual(packagingServicesVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).PackagingServices_Version);
        Assert.AreEqual(xsapiVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).XSAPI_Version);
        Assert.AreEqual(xcrdapiVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).XCRDAPI_Version);
        Assert.AreEqual(xcitreeVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).XCITREE_Version);
        Assert.AreEqual(windowsVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).Windows_Version);
        Assert.AreEqual(windows10Sdk, ((ValidatorComponentToolsCheck)_validator.Components[3]).Windows_10_SDK);
        Assert.AreEqual(grtsVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).GRTS_Version);
        Assert.AreEqual(xcapiVersion, ((ValidatorComponentToolsCheck)_validator.Components[3]).XCAPI_Version);
    }
}
