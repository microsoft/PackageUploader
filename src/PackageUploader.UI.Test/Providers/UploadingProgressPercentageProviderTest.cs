using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Test.Providers;

[TestClass]
public class UploadingProgressPercentageProviderTest
{

    private UploadingProgressPercentageProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _provider = new();
    }

    [TestMethod]
    public void TestConstructor()
    {
        var provider = new UploadingProgressPercentageProvider();
        Assert.AreEqual(PackageUploadingProgressStage.NotStarted, provider.UploadStage);
        Assert.AreEqual(0, provider.UploadingProgressPercentage);
        Assert.IsFalse(provider.UploadingCancelled);
    }

    [TestMethod]
    public void TestSetUploadStage()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UploadStage")
                eventRaised = true;
        };

        _provider.UploadStage = PackageUploadingProgressStage.ProcessingPackage;
        var stage = _provider.UploadStage;
        Assert.AreEqual(PackageUploadingProgressStage.ProcessingPackage, stage);
        Assert.IsTrue(eventRaised);

        eventRaised = false;
        _provider.UploadStage = PackageUploadingProgressStage.ProcessingPackage;
        Assert.AreEqual(PackageUploadingProgressStage.ProcessingPackage, _provider.UploadStage);
        Assert.IsFalse(eventRaised);

    }

    [TestMethod]
    public void TestSetUploadingCancelled()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UploadingCancelled")
                eventRaised = true;
        };

        _provider.UploadingCancelled = true;
        var isCancelled = _provider.UploadingCancelled;
        Assert.IsTrue(isCancelled);
        Assert.IsTrue(eventRaised);

        eventRaised = false;
        _provider.UploadingCancelled = true;
        Assert.IsTrue(_provider.UploadingCancelled);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestSetUploadingProgressPercentage()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UploadingProgressPercentage")
                eventRaised = true;
        };

        _provider.UploadingProgressPercentage = 50;
        var percentage = _provider.UploadingProgressPercentage;
        Assert.AreEqual(50, percentage);
        Assert.IsTrue(eventRaised);

        eventRaised = false;
        _provider.UploadingProgressPercentage = 50;
        Assert.AreEqual(50, _provider.UploadingProgressPercentage);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestSetUploadProgress()
    {
        bool eventRaised = false;
        _provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "UploadProgress")
                eventRaised = true;
        };

        var progress = new PackageUploadingProgress
        {
            Percentage = 50,
            Stage = PackageUploadingProgressStage.ProcessingPackage
        };
        _provider.UploadProgress = progress;
        Assert.AreEqual(progress, _provider.UploadProgress);
        Assert.IsTrue(eventRaised);

        eventRaised = false;
        _provider.UploadProgress = progress;
        Assert.AreEqual(progress, _provider.UploadProgress);
        Assert.IsFalse(eventRaised);
    }

}
