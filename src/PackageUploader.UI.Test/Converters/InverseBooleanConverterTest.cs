using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class InverseBooleanConverterTest
{
    private InverseBooleanConverter _converter;
    [TestInitialize]
    public void Setup()
    {
        _converter = new InverseBooleanConverter();
    }

    [TestMethod]
    public void TestConvertNotBool()
    {
        var result = _converter.Convert("not a bool", null, null, null);
        Assert.AreEqual("not a bool", result);
    }

    [TestMethod]
    public void TestConvertTrue()
    {
        var result = _converter.Convert(true, null, null, null);
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void TestConvertFalse()
    {
        var result = _converter.Convert(false, null, null, null);
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void TestConvertBackNotBool()
    {
        var result = _converter.ConvertBack("not a bool", null, null, null);
        Assert.AreEqual("not a bool", result);
    }

    [TestMethod]
    public void TestConvertBackTrue()
    {
        var result = _converter.ConvertBack(true, null, null, null);
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void TestConvertBackFalse()
    {
        var result = _converter.ConvertBack(false, null, null, null);
        Assert.AreEqual(true, result);
    }
}
