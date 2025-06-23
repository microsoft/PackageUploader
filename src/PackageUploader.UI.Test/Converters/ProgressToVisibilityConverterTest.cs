using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;
using System;
using System.Windows;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class ProgressToVisibilityConverterTest
{
    private ProgressToVisibilityConverter _converter;
    [TestInitialize]
    public void Setup()
    {
        _converter = new ProgressToVisibilityConverter();
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
    public void TestConvertInvalidValue()
    {
        // Act
        var result = _converter.Convert("invalid", null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertIntNotString()
    {
        // Act
        var result = _converter.Convert(50, null, 10, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertIntNotTwoValue()
    {
        // Act
        var result = _converter.Convert(50, null, "10", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertIntTwoValueNotInt()
    {
        // Act
        var result = _converter.Convert(50, null, "10-invalid", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertIntTwoValueValid()
    {
        // Act
        var result = _converter.Convert(50, null, "10-100", null);
        // Assert
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertIntTwoValueValidNotInRange()
    {
        // Act
        var result = _converter.Convert(5, null, "10-100", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void TestConvertBack()
    {
        // Act
        _converter.ConvertBack(null, null, null, null);
    }
}
