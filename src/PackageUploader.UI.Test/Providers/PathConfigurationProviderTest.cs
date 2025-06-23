using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Test.Providers;

[TestClass]
public class PathConfigurationProviderTest
{
    private PathConfigurationProvider _pathConfigurationProvider;
    
    [TestInitialize]
    public void Setup()
    {
        _pathConfigurationProvider = new PathConfigurationProvider();
    }
    
    [TestMethod]
    public void TestInitializeMethod()
    {
        var provider = new PathConfigurationProvider();
        Assert.AreEqual(string.Empty, provider.MakePkgPath);
        Assert.AreEqual(string.Empty, provider.PackageUploaderPath);
    }

    [TestMethod]
    public void TestSetMakePkgPath()
    {
        bool eventRaised = false;
        _pathConfigurationProvider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "MakePkgPath")
                eventRaised = true;
        };
        
        _pathConfigurationProvider.MakePkgPath = "C:\\makepkg";
        var makePkgPath = _pathConfigurationProvider.MakePkgPath;
        Assert.AreEqual("C:\\makepkg", makePkgPath);
        Assert.IsTrue(eventRaised);

        eventRaised = false;
        _pathConfigurationProvider.MakePkgPath = "C:\\makepkg";
        Assert.AreEqual("C:\\makepkg", _pathConfigurationProvider.MakePkgPath);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestSetPackageUploaderPath()
    {
        bool eventRaised = false;
        _pathConfigurationProvider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "PackageUploaderPath")
                eventRaised = true;
        };
        
        _pathConfigurationProvider.PackageUploaderPath = "C:\\packageuploader";
        var packageUploaderPath = _pathConfigurationProvider.PackageUploaderPath;
        Assert.AreEqual("C:\\packageuploader", packageUploaderPath);
        Assert.IsTrue(eventRaised);
        
        eventRaised = false;
        _pathConfigurationProvider.PackageUploaderPath = "C:\\packageuploader";
        Assert.AreEqual("C:\\packageuploader", _pathConfigurationProvider.PackageUploaderPath);
        Assert.IsFalse(eventRaised);
    }
}
