using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Test.Providers;

[TestClass]
public class PackingProgressPercentageProviderTest
{
    private PackingProgressPercentageProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _provider = new();
    }

    [TestMethod]
    public void InitializeTest()
    {

        var provider = new PackingProgressPercentageProvider();


        var percentage = provider.PackingProgressPercentage;
        var isCancelled = provider.PackingCancelled;


        Assert.AreEqual(0, percentage);
        Assert.IsFalse(isCancelled);
    }

    [TestMethod]
    public void SetPackingProgressPercentageTest()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "PackingProgressPercentage")
                eventRaised = true;
        };
        
        _provider.PackingProgressPercentage = 50;
        var percentage = _provider.PackingProgressPercentage;
        Assert.AreEqual(50, percentage);
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void SetPackingCancelledTest()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "PackingCancelled")
                eventRaised = true;
        };

        _provider.PackingCancelled = true;
        var isCancelled = _provider.PackingCancelled;
        Assert.IsTrue(isCancelled);
        Assert.IsTrue(eventRaised);
    }
}
