using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PackageUploader.UI.Test;

[TestClass]
public class MainWindowTest
{
    [TestMethod]
    [DataRow("Jason Williams (Xbox)", "JW")]
    [DataRow("Jason Williams (XBOX) (External)", "JW")]
    [DataRow("Jason Williams", "JW")]
    [DataRow("Jason", "J")]
    [DataRow("(Xbox)", "U")]
    [DataRow("", "U")]
    public void GetInitials_IgnoresTrailingParentheticalQualifiers(string userName, string expected)
    {
        Assert.AreEqual(expected, MainWindow.GetInitials(userName));
    }

    [TestMethod]
    [DataRow(1080, 880)]
    [DataRow(900, 880)]
    [DataRow(850, 850)]
    [DataRow(800, 800)]
    public void GetStandardWindowHeight_ClampsToWorkingArea(double workAreaHeight, double expected)
    {
        Assert.AreEqual(expected, MainWindow.GetStandardWindowHeight(workAreaHeight));
    }
}
