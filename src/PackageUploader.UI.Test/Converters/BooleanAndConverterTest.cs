using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Converters;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class BooleanAndConverterTest
{
    [TestMethod]
    public void TestNullConvert()
    {
        var converter = new BooleanAndConverter();
        Assert.IsFalse((bool)converter.Convert(null, null, null, null));
    }

    [TestMethod]
    public void TestNoValuesConvert()
    {
        var converter = new BooleanAndConverter();
        Assert.IsFalse((bool)converter.Convert(new object[0], null, null, null));
    }

    [TestMethod]
    public void TestSingleValue()
    {
        var converter = new BooleanAndConverter();
        object[] values = new object[] { true };
        Assert.IsTrue((bool)converter.Convert(values, null, null, null));
    }

    [TestMethod]
    public void TestMultipleValuesAllTrue()
    {
        var converter = new BooleanAndConverter();
        object[] values = new object[] { true, true };
        Assert.IsTrue((bool)converter.Convert(values, null, null, null));
    }

    [TestMethod]
    public void TestMultipleValuesOneFalse()
    {
        var converter = new BooleanAndConverter();
        object[] values = new object[] { true, false };
        Assert.IsFalse((bool)converter.Convert(values, null, null, null));
    }

    [TestMethod]
    public void TestMultipleValuesAllFalse()
    {
        var converter = new BooleanAndConverter();
        object[] values = new object[] { false, false };
        Assert.IsFalse((bool)converter.Convert(values, null, null, null));
    }

    [TestMethod]
    public void TestBoolValuesRequired()
    {
        var converter = new BooleanAndConverter();
        object[] values = new object[] { true, 1 };
        Assert.IsFalse((bool)converter.Convert(values, null, null, null));
    }

    [TestMethod]
    [ExpectedException(typeof(System.NotImplementedException))]
    public void TestMethod2()
    {
        var converter = new BooleanAndConverter();
        converter.ConvertBack(null, null, null, null);
    }
}
