using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace PackageUploader.UI.Test.Model;

[TestClass]
public class PartialGameConfigModelTest
{
    private string _goodConfigPath;
    private string _badConfigPath;

    private string _goodConfigContent;

    private string _IdentityName;
    private string _IdentityPublisher;
    private string _IdentityVersion;
    private string _ExecutableId;
    private string _ExecutableName;
    private string _ExecutableTargetDeviceFamily;
    private string _ShellVisualsDefaultDisplayName;
    private string _ShellVisualsPublisherDisplayName;
    private string _ShellVisualsStoreLogo;
    private string _ShellVisualsSquare150x150Logo;
    private string _ShellVisualsSquare44x44Logo;
    private string _ShellVisualsSplashScreenImage;
    private string _ShellVisualsDescription;
    private string _MSAAppId;
    private string _TitleId;
    private string _StoreId;


    [TestInitialize]
    public void Setup()
    {
        _goodConfigPath = Path.GetTempFileName();
        _badConfigPath = Path.GetTempFileName();

        _IdentityName = RandomString(Random.Shared.Next(10, 100));
        _IdentityPublisher = RandomString(Random.Shared.Next(10, 100));
        _IdentityVersion = $"{Random.Shared.Next(100)}.{Random.Shared.Next(100)}.{Random.Shared.Next(100)}.{Random.Shared.Next(100)}";
        _ExecutableId = RandomString(Random.Shared.Next(10, 100));
        _ExecutableName = _ExecutableId + ".exe";
        _ExecutableTargetDeviceFamily = RandomDeviceFamily();
        _ShellVisualsDefaultDisplayName = RandomString(Random.Shared.Next(10, 100));
        _ShellVisualsPublisherDisplayName = RandomString(Random.Shared.Next(10, 100));
        _ShellVisualsStoreLogo = Path.GetTempFileName();
        _ShellVisualsSquare150x150Logo = Path.GetTempFileName();
        _ShellVisualsSquare44x44Logo = Path.GetTempFileName();
        _ShellVisualsSplashScreenImage = Path.GetTempFileName();
        _ShellVisualsDescription = RandomString(Random.Shared.Next(10, 100));
        _MSAAppId = RandomString(Random.Shared.Next(10, 100));
        _TitleId = RandomString(Random.Shared.Next(10, 100));
        _StoreId = RandomString(Random.Shared.Next(10, 100));

        _goodConfigContent = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Game ConfigVersion="0">
                <Identity Name="{_IdentityName}" 
                          Publisher="{_IdentityPublisher}" 
                          Version="{_IdentityVersion}" />
                <ExecutableList>
                    <Executable Id="{_ExecutableId}" 
                                Name="{_ExecutableName}" 
                                TargetDeviceFamily="{_ExecutableTargetDeviceFamily}" />
                </ExecutableList>
                
                <ShellVisuals DefaultDisplayName="{_ShellVisualsDefaultDisplayName}" 
                              PublisherDisplayName="{_ShellVisualsPublisherDisplayName}" 
                              StoreLogo="{_ShellVisualsStoreLogo}" 
                              Square150x150Logo="{_ShellVisualsSquare150x150Logo}" 
                              Square44x44Logo="{_ShellVisualsSquare44x44Logo}" 
                              SplashScreenImage="{_ShellVisualsSplashScreenImage}" 
                              Description="{_ShellVisualsDescription}" />
                <MSAAppId>{_MSAAppId}</MSAAppId>
                <TitleId>{_TitleId}</TitleId>
                <StoreId>{_StoreId}</StoreId>
            </Game>
            """;

        File.WriteAllText(_goodConfigPath, _goodConfigContent);
    }

    public string getGoodConfigPath()
    {
        return _goodConfigPath;
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void TestInvalidConstructor()
    {
        _ = new PartialGameConfigModel(null);
        _ = new PartialGameConfigModel("");
        _ = new PartialGameConfigModel("C:\\nonexistent\\path\\to\\MicrosoftGame.config");
    }

    [TestMethod]
    public void TestInvalidConfigFile()
    {
        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "This is not a valid MicrosoftGame.config file");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <Hello> </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <Identity& /> </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <<Identity /> </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <Identity ' /> </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <Identity Hello=\"yes\"\"/>  </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
                finally
        {
            File.Delete(_badConfigPath);
        }

        try
        {
            // Assemble
            File.WriteAllText(_badConfigPath, "<Game> <Identity > />  </Game>");
            // Act
            _ = new PartialGameConfigModel(_badConfigPath);
            // Assert
            Assert.Fail("Expected Exception to be thrown");
        }
        catch (XmlException e)
        {
            // good
        }
        finally
        {
            File.Delete(_badConfigPath);
        }
    }


        [TestMethod]
    public void TestGoodConfig()
    {
        var model = new PartialGameConfigModel(_goodConfigPath);
        Assert.AreEqual(_IdentityName, model.Identity.Name);
        Assert.AreEqual(_IdentityPublisher, model.Identity.Publisher);
        Assert.AreEqual(_IdentityVersion, model.Identity.Version);
        Assert.AreEqual(1, model.Executables.Count);
        Assert.AreEqual(_ExecutableId, model.Executables[0].Id);
        Assert.AreEqual(_ExecutableName, model.Executables[0].Name);
        Assert.AreEqual(_ExecutableTargetDeviceFamily, model.Executables[0].TargetDeviceFamily);
        Assert.AreEqual(_ShellVisualsDefaultDisplayName, model.ShellVisuals.DefaultDisplayName);
        Assert.AreEqual(_ShellVisualsPublisherDisplayName, model.ShellVisuals.PublisherDisplayName);
        Assert.AreEqual(_ShellVisualsStoreLogo, model.ShellVisuals.StoreLogo);
        Assert.AreEqual(_ShellVisualsSquare150x150Logo, model.ShellVisuals.Square150x150Logo);
        Assert.AreEqual(_ShellVisualsSquare44x44Logo, model.ShellVisuals.Square44x44Logo);
        Assert.AreEqual(_ShellVisualsSplashScreenImage, model.ShellVisuals.SplashScreenImage);
        Assert.AreEqual(_ShellVisualsDescription, model.ShellVisuals.Description);
        Assert.AreEqual(_MSAAppId, model.MSAAppId);
        Assert.AreEqual(_TitleId, model.TitleId);
        Assert.AreEqual(_StoreId, model.StoreId);
    }

    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(_goodConfigPath);
        File.Delete(_badConfigPath);
        File.Delete(_ShellVisualsStoreLogo);
        File.Delete(_ShellVisualsSquare150x150Logo);
        File.Delete(_ShellVisualsSquare44x44Logo);
        File.Delete(_ShellVisualsSplashScreenImage);
    }


    private static string RandomString(int length)
    {
        // Thanks Copilot
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    private static string RandomDeviceFamily()
    {
        var deviceFamilies = new string[] { "PC", "Console" };
        return deviceFamilies[Random.Shared.Next(deviceFamilies.Length)];
    }
}
