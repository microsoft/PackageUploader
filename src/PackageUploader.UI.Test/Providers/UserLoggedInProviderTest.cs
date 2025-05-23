using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Test;

[TestClass]
public class UserLoggedInProviderTest
{
    private UserLoggedInProvider _provider;
    [TestInitialize]
    public void Setup()
    {
        _provider = new();
    }
    
    [TestMethod]
    public void TestInitialize()
    {
        var provider = new UserLoggedInProvider();
        Assert.IsFalse(provider.UserLoggedIn);
        Assert.IsTrue(string.IsNullOrEmpty(provider.UserName));
        Assert.IsTrue(string.IsNullOrEmpty(provider.AccessToken));
    }

    [TestMethod]
    public void TestSetUserLoggedIn()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UserLoggedIn")
                eventRaised = true;
        };
        
        _provider.UserLoggedIn = true;
        var userLoggedIn = _provider.UserLoggedIn;
        Assert.IsTrue(userLoggedIn);
        Assert.IsTrue(eventRaised);
        
        eventRaised = false;
        _provider.UserLoggedIn = true;
        Assert.IsTrue(_provider.UserLoggedIn);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestSetUserName()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UserName")
                eventRaised = true;
        };
        
        _provider.UserName = "test";
        var userName = _provider.UserName;
        Assert.AreEqual("test", userName);
        Assert.IsTrue(eventRaised);
        
        eventRaised = false;
        _provider.UserName = "test";
        Assert.AreEqual("test", _provider.UserName);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestSetAccessToken()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "AccessToken")
                eventRaised = true;
        };
        
        _provider.AccessToken = "test";
        var accessToken = _provider.AccessToken;
        Assert.AreEqual("test", accessToken);
        Assert.IsTrue(eventRaised);
        
        eventRaised = false;
        _provider.AccessToken = "test";
        Assert.AreEqual("test", _provider.AccessToken);
        Assert.IsFalse(eventRaised);
    }
}
