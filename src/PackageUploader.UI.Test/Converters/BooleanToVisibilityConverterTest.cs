using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using PackageUploader.UI.Converters;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class BooleanToVisibilityConverterTest
{
    private BooleanToVisibilityConverter _converter;

    [TestInitialize]
    public void Setup()
    {
        _converter = new BooleanToVisibilityConverter();
    }

    [TestMethod]
    public void TestConvertNotBool()
    {
        var result = _converter.Convert("not a bool", null, null, null);

        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertTrue()
    {
        var result = _converter.Convert(true, null, null, null);
        Assert.AreEqual(Visibility.Visible, result);
    }
    
    [TestMethod]
    public void TestConvertFalse()
    {
        var result = _converter.Convert(false, null, null, null);
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertInverted_Invert()
    {
        var result = _converter.Convert(true, null, "Invert", null);
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertInverted_True()
    {
        var result = _converter.Convert(true, null, "true", null);
        Assert.AreEqual(Visibility.Collapsed, result);
    }
    
    [TestMethod]
    public void TestConvertInverted_False_Invert()
    {
        var result = _converter.Convert(false, null, "Invert", null);
        Assert.AreEqual(Visibility.Visible, result);
    } 
    
    [TestMethod]
    
    public void TestConvertInverted_False_True()
    {
        var result = _converter.Convert(false, null, "true", null);
        Assert.AreEqual(Visibility.Visible, result);
    }

    [TestMethod]
    public void TestConvertBackNotVisibility()
    {
        var result = _converter.ConvertBack("not a visibility", null, null, null);
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void TestConvertBackVisible()
    {
        var result = _converter.ConvertBack(Visibility.Visible, null, null, null);
        Assert.IsTrue((bool)result);
    }

    [TestMethod]
    public void TestConvertBackCollapsed()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, null, null, null);
        Assert.IsFalse((bool)result);
    }

    [TestMethod]
    public void TestConvertBackInverted_Invert()
    {
        var result = _converter.ConvertBack(Visibility.Visible, null, "Invert", null);
        Assert.IsFalse((bool)result);
    }
    [TestMethod]
    public void TestConvertBackInverted_True()
    {
        var result = _converter.ConvertBack(Visibility.Visible, null, "true", null);
        Assert.IsFalse((bool)result);
    }

    [TestMethod]
    public void TestConvertBackInverted_Collapsed_Invert()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, null, "Invert", null);
        Assert.IsTrue((bool)result);
    }
    [TestMethod]
    public void TestConvertBackInverted_Collapsed_True()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, null, "true", null);
        Assert.IsTrue((bool)result);
    }
}
