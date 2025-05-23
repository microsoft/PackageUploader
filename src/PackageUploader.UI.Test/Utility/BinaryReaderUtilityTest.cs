using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Utility;

namespace PackageUploader.UI.Test;

[TestClass]
public class BinaryReaderUtilityTest
{
    [TestMethod]
    public void TestReadNullTerminatedString()
    {
        var reader = new System.IO.BinaryReader(new System.IO.MemoryStream());
        var str = "Hello, World";
        reader.BaseStream.Write(System.Text.Encoding.Unicode.GetBytes(str));
        reader.BaseStream.WriteByte(0);
        reader.BaseStream.Position = 0;

        var readStr = BinaryReaderUtility.ReadNullTerminatedString(reader, str.Length);
        Assert.IsTrue(string.Equals(str, readStr));
    }
}
