using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Utility;
using System;

namespace PackageUploader.UI.Test.Utility;

[TestClass]
public class GuidUtilityTest
{
    [TestMethod]
    public void TestReadGuid()
    {
        var guid = new Guid("00000000-0000-0000-0000-000000000000");
        var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(guid.ToByteArray()));
        Assert.AreEqual(guid, GuidUtility.ReadGuid(reader));

        guid = Guid.NewGuid();
        reader = new System.IO.BinaryReader(new System.IO.MemoryStream(guid.ToByteArray()));
        Assert.AreEqual(guid, GuidUtility.ReadGuid(reader));
    }

    [TestMethod]
    public void TestReadGuids()
    {
        // Thanks copilot?
        var guids = new Lazy<Guid[]>(() => new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }).Value;
        var reader = new System.IO.BinaryReader(new System.IO.MemoryStream());
        foreach (var guid in guids)
        {
            reader.BaseStream.Write(guid.ToByteArray(), 0, guid.ToByteArray().Length);
        }
        reader.BaseStream.Position = 0;
        var readGuids = GuidUtility.ReadGuids(reader, guids.Length);
        for(int i = 0; i < guids.Length; ++i)
        {
            Assert.AreEqual(guids[i], readGuids[i]);
        }
    }
}
