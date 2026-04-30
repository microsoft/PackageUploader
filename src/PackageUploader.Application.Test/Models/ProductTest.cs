// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.Application.Models;
using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.Application.Test.Models;

[TestClass]
public class ProductTest
{
    private static GameProduct CreateGameProduct(string productId = "prod-1", string bigId = "big-1", string productName = "TestGame")
    {
        var product = (GameProduct)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(GameProduct));
        typeof(GameProduct).GetProperty("ProductId")!.SetValue(product, productId);
        typeof(GameProduct).GetProperty("BigId")!.SetValue(product, bigId);
        typeof(GameProduct).GetProperty("ProductName")!.SetValue(product, productName);
        return product;
    }

    [TestMethod]
    public void Constructor_SortsBranchesAndFlights()
    {
        var gameProduct = CreateGameProduct();

        var branch1 = new Mock<IGamePackageBranch>();
        branch1.Setup(b => b.Name).Returns("main");
        branch1.Setup(b => b.BranchType).Returns(GamePackageBranchType.Branch);

        var flight1 = new Mock<IGamePackageBranch>();
        flight1.Setup(b => b.Name).Returns("flight-alpha");
        flight1.Setup(b => b.BranchType).Returns(GamePackageBranchType.Flight);

        var branch2 = new Mock<IGamePackageBranch>();
        branch2.Setup(b => b.Name).Returns("preview");
        branch2.Setup(b => b.BranchType).Returns(GamePackageBranchType.Branch);

        var branches = new List<IGamePackageBranch> { branch1.Object, flight1.Object, branch2.Object };

        var product = new Product(gameProduct, branches);

        Assert.AreEqual("prod-1", product.ProductId);
        Assert.AreEqual("big-1", product.BigId);
        Assert.AreEqual("TestGame", product.ProductName);

#pragma warning disable CA1861 // Avoid constant arrays as arguments
        CollectionAssert.AreEqual(new[] { "main", "preview" }, (List<string>)product.BranchFriendlyNames);
        CollectionAssert.AreEqual(new[] { "flight-alpha" }, (List<string>)product.FlightNames);
#pragma warning restore CA1861 // Avoid constant arrays as arguments
    }

    [TestMethod]
    public void Constructor_NoBranches_EmptyLists()
    {
        var gameProduct = CreateGameProduct();

        var product = new Product(gameProduct, []);

        Assert.IsEmpty(product.BranchFriendlyNames);
        Assert.IsEmpty(product.FlightNames);
    }

    [TestMethod]
    public void ToJson_ValidProduct_ReturnsValidJson()
    {
        var gameProduct = CreateGameProduct();
        var product = new Product(gameProduct, []);

        var json = product.ToJson();

        Assert.Contains("prod-1", json);
        Assert.Contains("big-1", json);
        Assert.Contains("TestGame", json);
    }

    [TestMethod]
    public void Constructor_AllFlights_NoBranches()
    {
        var gameProduct = CreateGameProduct();

        var flight1 = new Mock<IGamePackageBranch>();
        flight1.Setup(b => b.Name).Returns("flight-1");
        flight1.Setup(b => b.BranchType).Returns(GamePackageBranchType.Flight);

        var flight2 = new Mock<IGamePackageBranch>();
        flight2.Setup(b => b.Name).Returns("flight-2");
        flight2.Setup(b => b.BranchType).Returns(GamePackageBranchType.Flight);

        var product = new Product(gameProduct, [flight1.Object, flight2.Object]);

        Assert.IsEmpty(product.BranchFriendlyNames);
        Assert.HasCount(2, product.FlightNames);
    }
}
