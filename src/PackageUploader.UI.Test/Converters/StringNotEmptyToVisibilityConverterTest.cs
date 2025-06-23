using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;


using System;
using System.Windows;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class StringNotEmptyToVisibilityConverterTest
{
    private StringNotEmptyToVisibilityConverter _converter;
    [TestInitialize]
    public void Setup()
    {
        _converter = new StringNotEmptyToVisibilityConverter();
    }

    [TestMethod]
    public void TestConvertNull()
    {
        // Act
        var result = _converter.Convert(null, null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertNotString()
    {
        // Act
        var result = _converter.Convert(10, null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertEmptyString()
    {
        // Act
        var result = _converter.Convert(string.Empty, null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertString()
    {
        // Act
        var result = _converter.Convert("test", null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_BadInvertString()
    {
        // Act
        var result = _converter.Convert("test", null, "hello", null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_BadInvertNotString()
    {
        // Act
        var result = _converter.Convert("test", null, 10, null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_BadInvertEmptyString()
    {
        // Act
        var result = _converter.Convert("test", null, string.Empty, null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringInvert_Collapsed()
    {
        // Act
        var result = _converter.Convert("test", null, "Invert", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringTrue_Collapsed()
    {
        // Act
        var result = _converter.Convert("test", null, "true", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringInvert_Visible_Null()
    {
        // Act
        var result = _converter.Convert(null, null, "Invert", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result); // TODO: because null string is always collapsed?
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringInvert_Visible_Empty()
    {
        // Act
        var result = _converter.Convert(string.Empty, null, "Invert", null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringTrue_Visible_Null()
    {
        // Act
        var result = _converter.Convert(null, null, "true", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result); // TODO: because null string is always collapsed?
    }

    [TestMethod]
    public void TestConvertStringInverted_String_GoodInvert_StringTrue_Visible_Empty()
    {
        // Act
        var result = _converter.Convert(string.Empty, null, "true", null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }



    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void TestConvertBack()
    {
        // Act
        _converter.ConvertBack(null, null, null, null);
    }
}
