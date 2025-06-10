using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;

namespace PackageUploader.UI.Test.Model;

[TestClass]
public class ErrorModelTest
{

    private ErrorModel _errorModel;

    [TestInitialize]
    public void Setup()
    {
        _errorModel = new ErrorModel();
    }

    [TestMethod]
    public void TestMainMessage()
    {
        var Value = "Test Main Message";
        _errorModel.MainMessage = Value;
        Assert.AreEqual(Value, _errorModel.MainMessage);
    }

    [TestMethod]
    public void TestDetailMessage()
    {
        var Value = "Test Detail Message";
        _errorModel.DetailMessage = Value;
        Assert.AreEqual(Value, _errorModel.DetailMessage);
    }

    [TestMethod]
    public void TestOriginPage()
    {
        var Value = typeof(object);
        _errorModel.OriginPage = Value;
        Assert.AreEqual(Value, _errorModel.OriginPage);
    }

    [TestMethod]
    public void TestLogsPath()
    {
        var Value = "Test Logs Path";
        _errorModel.LogsPath = Value;
        Assert.AreEqual(Value, _errorModel.LogsPath);
    }
}
