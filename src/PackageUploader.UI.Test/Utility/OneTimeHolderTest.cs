using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using PackageUploader.UI.Utility;
using System.Numerics;

namespace PackageUploader.UI.Test;

[TestClass]
public class OneTimeHolderTest
{
    [TestMethod]
    public void TestGetter()
    {
        OneTimeHolder<String> holder = new OneTimeHolder<String>("Hello, World");
        Assert.AreEqual("Hello, World", holder.Value);
        Assert.IsNull(holder.Value);
    }
}
