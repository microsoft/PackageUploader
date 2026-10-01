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
}
