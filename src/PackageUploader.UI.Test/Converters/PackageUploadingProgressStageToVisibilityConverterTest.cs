using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Converters;
using System;
using System.Windows;

namespace PackageUploader.UI.Test.Converters;

[TestClass]
public class PackageUploadingProgressStageToVisibilityConverterTest
{
    private PackageUploadingProgressStageToVisibilityConverter _converter;
    [TestInitialize]
    public void Setup()
    {
        _converter = new PackageUploadingProgressStageToVisibilityConverter();
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
    public void TestConvertStageNotString()
    {
        // Act
        var result = _converter.Convert(PackageUploadingProgressStage.NotStarted, null, null, null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertStageStringNotValid()
    {
        // Act
        var result = _converter.Convert(PackageUploadingProgressStage.NotStarted, null, "invalid", null);
        // Assert
        Assert.AreEqual(Visibility.Collapsed, result);
    }

    [TestMethod]
    public void TestConvertStageStringValidSingle()
    {
        var values = PackageUploadingProgressStage.GetValues(typeof(PackageUploadingProgressStage));
        foreach (var value in values)
        {
            // Act
            var result = _converter.Convert(value, null, value.ToString(), null);
            // Assert
            Assert.AreEqual(Visibility.Visible, result);
        }

        for(int i = 1; i < values.Length; i++)
        {
            for(int j = i+1; j < values.Length; j++)
            {
                // Act
                var result = _converter.Convert(values.GetValue(i), null, values.GetValue(j).ToString(), null);
                // Assert
                Assert.AreEqual(Visibility.Collapsed, result);
            }
        }
    }

    [TestMethod]
    public void TestConvertStageStringValidDouble()
    {
        var values = PackageUploadingProgressStage.GetValues(typeof(PackageUploadingProgressStage));
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = i; j < values.Length; j++)
            {
                // Act
                var result = _converter.Convert(values.GetValue(i), null, $"{values.GetValue(i)}-{values.GetValue(j)}", null);
                // Assert
                Assert.AreEqual(Visibility.Visible, result);
            }
        }
        for (int i = 0; i < values.Length; i++)
        {
            for(int j = i+1; j < values.Length; j++)
            {
                // below the range
                for (int k = j+1; k < values.Length; k++)
                {
                    // Act
                    var result = _converter.Convert(values.GetValue(k), null, $"{values.GetValue(i)}-{values.GetValue(j)}", null);
                    // Assert
                    Assert.AreEqual(Visibility.Collapsed, result);
                }
                // above the range
                for (int k = 0; k < i; k++)
                {
                    // Act
                    var result = _converter.Convert(values.GetValue(k), null, $"{values.GetValue(i)}-{values.GetValue(j)}", null);
                    // Assert
                    Assert.AreEqual(Visibility.Collapsed, result);
                }
            }
        }


    }

    [TestMethod]
    [ExpectedException(typeof(NotImplementedException))]
    public void ConvertBack()
    {
        _converter.ConvertBack(null, null, null, null);
    }
}
