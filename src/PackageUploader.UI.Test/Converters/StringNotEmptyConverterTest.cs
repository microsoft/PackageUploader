using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;
using System;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class StringNotEmptyConverterTest
{
    private StringNotEmptyConverter _converter;
    [TestInitialize]
    public void Setup()
    {
        _converter = new StringNotEmptyConverter();
    }

    [TestMethod]
    public void TestConvertNull()
    {
        // Act
        var result = _converter.Convert(null, null, null, null);
        // Assert
        Assert.IsFalse((bool)result);
    }

    [TestMethod]
    public void TestConvertEmptyString()
    {
        // Act
        var result = _converter.Convert(string.Empty, null, null, null);
        // Assert
        Assert.IsFalse((bool)result);
    }

    [TestMethod]
    public void TestConvertString()
    {
        // Act
        var result = _converter.Convert("test", null, null, null);
        // Assert
        Assert.IsTrue((bool)result);
    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void TestConvertBack()
    {
        // Act
        _converter.ConvertBack(null, null, null, null);
    }
}
