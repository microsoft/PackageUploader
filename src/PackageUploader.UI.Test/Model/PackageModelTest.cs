using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;

namespace PackageUploader.UI.Test.Model;

[TestClass]
public class PackageModelTest
{
    private PackageModel _packageModel;

    [TestInitialize]
    public void Setup()
    {
        _packageModel = new PackageModel();
    }

    [TestMethod]
    public void TestPackageFilePath()
    {
        // Arrange
        var expectedPath = @"C:\Packages\TestPackage.xvc";

        // Act
        _packageModel.PackageFilePath = expectedPath;

        // Assert
        Assert.AreEqual(expectedPath, _packageModel.PackageFilePath);
    }

    [TestMethod]
    public void TestGameConfigFilePath()
    {
        // Arrange
        var expectedPath = @"C:\Game\MicrosoftGame.config";

        // Act
        _packageModel.GameConfigFilePath = expectedPath;

        // Assert
        Assert.AreEqual(expectedPath, _packageModel.GameConfigFilePath);
    }

    [TestMethod]
    public void TestPackageType()
    {
        // Arrange
        var expectedType = "Xbox";

        // Act
        _packageModel.PackageType = expectedType;

        // Assert
        Assert.AreEqual(expectedType, _packageModel.PackageType);
    }

    [TestMethod]
    public void TestPackageSize()
    {
        // Arrange
        var expectedSize = "5.2 GB";

        // Act
        _packageModel.PackageSize = expectedSize;

        // Assert
        Assert.AreEqual(expectedSize, _packageModel.PackageSize);
    }

    [TestMethod]
    public void TestPackageBigId()
    {
        // Arrange
        var expectedBigId = "12345678-1234-1234-1234-123456789012";
        // Act
        _packageModel.BigId = expectedBigId;
        // Assert
        Assert.AreEqual(expectedBigId, _packageModel.BigId);
    }

    [TestMethod]
    public void TestPackageTitleId()
    {
        // Arrange
        var expectedTitleId = "12345678-1234-1234-1234-123456789012";
        // Act
        _packageModel.TitleId = expectedTitleId;
        // Assert
        Assert.AreEqual(expectedTitleId, _packageModel.TitleId);
    }

    [TestMethod]
    public void TestPackageVersion()
    {
        // Arrange
        var expectedVersion = "1.2.3";
        // Act
        _packageModel.Version = expectedVersion;
        // Assert
        Assert.AreEqual(expectedVersion, _packageModel.Version);
    }

    [TestMethod]
    public void TestPackageEkbFilePath()
    {
        // Arrange
        var expectedPath = @"C:\Packages\TestPackage.xvc";
        // Act
        _packageModel.EkbFilePath = expectedPath;
        // Assert
        Assert.AreEqual(expectedPath, _packageModel.EkbFilePath);
    }

    [TestMethod]
    public void TestPackageSubValFilePath()
    {
        // Arrange
        var expectedPath = @"C:\Packages\TestPackage.xvc";
        // Act
        _packageModel.SubValFilePath = expectedPath;
        // Assert
        Assert.AreEqual(expectedPath, _packageModel.SubValFilePath);
    }

    [TestMethod]
    public void TestPackageSymbolBundleFilePath()
    {
        // Arrange
        var expectedPath = @"C:\Packages\TestPackage.xvc";
        // Act
        _packageModel.SymbolBundleFilePath = expectedPath;
        // Assert
        Assert.AreEqual(expectedPath, _packageModel.SymbolBundleFilePath);
    }

    [TestMethod]
    public void TestBranchId()
    {
        // Arrange
        var expectedBranchId = "12345678-1234-1234-1234-123456789012";
        // Act
        _packageModel.BranchId = expectedBranchId;
        // Assert
        Assert.AreEqual(expectedBranchId, _packageModel.BranchId);
    }

    [TestMethod]
    public void TestPackagePreviewImage()
    {
        // Arrange
        var expectedImage = new System.Windows.Media.Imaging.BitmapImage();
        // Act
        _packageModel.PackagePreviewImage = expectedImage;
        // Assert
        Assert.AreEqual(expectedImage, _packageModel.PackagePreviewImage);
    }

    [TestMethod]
    public void TestDefaultValues()
    {
        // Arrange
        var freshModel = new PackageModel();

        // Assert
        Assert.AreEqual(string.Empty, freshModel.PackageFilePath);
        Assert.AreEqual(string.Empty, freshModel.GameConfigFilePath);
        Assert.AreEqual(string.Empty, freshModel.PackageType);
        Assert.AreEqual(string.Empty, freshModel.PackageSize);
        Assert.AreEqual(string.Empty, freshModel.BigId);
        Assert.AreEqual(string.Empty, freshModel.TitleId);
        Assert.AreEqual(string.Empty, freshModel.Version);
        Assert.AreEqual(string.Empty, freshModel.EkbFilePath);
        Assert.AreEqual(string.Empty, freshModel.SubValFilePath);
        Assert.AreEqual(string.Empty, freshModel.SymbolBundleFilePath);
        Assert.AreEqual(string.Empty, freshModel.BranchId);
        Assert.IsNull(freshModel.PackagePreviewImage);
    }

    // Don't care about these, I don't think
    // TODO: Determine if we care about these
    /*
    [TestMethod]
    public void TestEquality()
    {
        // Arrange
        var model1 = new PackageModel
        {
            PackageFilePath = "Path1",
            GameConfigFilePath = "Config1",
            PackageType = "Xbox",
            PackageSize = "5 GB"
        };

        var model2 = new PackageModel
        {
            PackageFilePath = "Path1",
            GameConfigFilePath = "Config1",
            PackageType = "Xbox",
            PackageSize = "5 GB"
        };

        var model3 = new PackageModel
        {
            PackageFilePath = "Path2",
            GameConfigFilePath = "Config1",
            PackageType = "Xbox",
            PackageSize = "5 GB"
        };

        // Assert
        Assert.AreEqual(model1, model2);
        Assert.AreNotEqual(model1, model3);
    }

    [TestMethod]
    public void TestGetHashCode_ReturnsSameValueForEqualObjects()
    {
        // Arrange
        var model1 = new PackageModel
        {
            PackageFilePath = "Path1",
            GameConfigFilePath = "Config1",
            PackageType = "Xbox",
            PackageSize = "5 GB"
        };

        var model2 = new PackageModel
        {
            PackageFilePath = "Path1",
            GameConfigFilePath = "Config1",
            PackageType = "Xbox",
            PackageSize = "5 GB"
        };

        // Assert
        Assert.AreEqual(model1.GetHashCode(), model2.GetHashCode());
    }*/
}
