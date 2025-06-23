using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;
using System;
using System.Windows.Media;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class BoolToColorConverterTest
{
    private BoolToColorConverter _converter;
    
    [TestInitialize]
    public void Setup()
    {
        _converter = new BoolToColorConverter();
    }

    [TestMethod]
    public void TestConvertNull()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert(null, typeof(Color), null, null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertNotBool()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert("not a bool", typeof(Color), null, null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertNotString()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert(true, typeof(Color), 123, null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertNotBoolAndNotString()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert(123, typeof(Color), 123, null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertNotEnoughColors()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert(true, typeof(Color), "Red", null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertNotBoolEnoughColors()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert(10, typeof(Color), "Red,Blue", null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertTrue()
    {
        Color expected = Colors.Red;
        var result = _converter.Convert(true, typeof(Color), "Red,Blue", null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertFalse()
    {
        Color expected = Colors.Blue;
        var result = _converter.Convert(false, typeof(Color), "Red,Blue", null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestConvertEnoughColorsNotBool()
    {
        Color expected = Colors.Transparent;
        var result = _converter.Convert("not a bool", typeof(Color), "Red,Blue", null);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void ConvertBackTest()
    {
        
        var input = true;
        var expectedOutput = "SomeColor";

        var result = _converter.ConvertBack(input, typeof(string), expectedOutput, null);
    }
}
