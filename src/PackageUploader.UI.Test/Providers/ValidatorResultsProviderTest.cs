using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;

namespace PackageUploader.UI.Test;

[TestClass]
public class ValidatorResultsProviderTest
{
    private ValidatorResultsProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _provider = new ValidatorResultsProvider();
    }

    [TestMethod]
    public void TestGetter()
    {
        Assert.IsNotNull(_provider.Results);
    }

    [TestMethod]
    public void TestSetter()
    {
        var results = new ValidatorResults();
        _provider.Results = results;
        Assert.AreEqual(results, _provider.Results);
    }

    [TestMethod]
    public void TestOnPropertyChanged()
    {
        // Setup 
        var eventWasRaised = false;
        _provider.PropertyChanged += (sender, args) =>
        {
            eventWasRaised = true;
        };

        // Action
        _provider.Results = new ValidatorResults();

        // Assert
        Assert.IsTrue(eventWasRaised);
    }

    [TestMethod]
    public void TestGetter2()
    {
        Assert.IsNotNull(_provider.Results);

        var results = _provider.Results;

        results.TotalErrors = 2;

        Assert.AreEqual(2, _provider.Results.TotalErrors);
    }
}
