using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model; 
using System.IO;
using System.Text;

namespace Package.UI.Test.Model.Xvc;



[TestClass]
public class UserDataHeaderTest
{
    //Define a function for writing a test stream with the given parameters. The goal is to try to read the created stream using the UserDataHeader.Read method.
    private Stream WriteTestStream(UInt32 headerLength, UInt32 headerVersion, UserDataType datatype, UInt32 dataLength, byte[] extraData = null)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(headerLength);
            writer.Write(headerVersion);
            writer.Write((uint)datatype);

            writer.Write(dataLength);

            if (extraData != null)
            {
                writer.Write(extraData);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private UserDataHeader _userHeader;

    [TestInitialize]
    public void Setup()
    {
        _userHeader = new UserDataHeader();

    }

    [TestMethod]

    public void TestReadValidHeader()
    {
        //Arrange
        uint headerLength = 18;
        uint headerVersion = UserDataHeader.XVD_USER_DATA_VERSION;
        uint dataLength = 16;
        UserDataType dataType = UserDataType.XvdUserDataPackageFiles;

        //Act
        Stream stream = WriteTestStream(headerLength, headerVersion, dataType, dataLength, new byte[] { 1, 2 });
        UserDataHeader header = UserDataHeader.Read(stream);

        //Assert
        Assert.IsNotNull(header, "Header should not be null for valid data.");
        Assert.AreEqual(headerLength, header.HeaderLength, "Header length does not match expected value.");
        Assert.AreEqual(headerVersion, header.HeaderVersion, "Header version does not match expected value.");
        Assert.AreEqual(dataType, header.DataType, "Data Type does not match expected value. ");

    }

    [TestMethod]

    public void TestReadInvalidHeaderLength()
    {
        //Arrange
        uint headerLength = 8; //invalid length, since it is less than 16
        uint headerVersion = UserDataHeader.XVD_USER_DATA_VERSION;
        uint dataLength = 16;
        UserDataType DataType = UserDataType.XvdUserDataPackageFiles;

        //Act
        Stream stream = WriteTestStream(headerLength, headerVersion, DataType, dataLength, new byte[] { 1, 2 });
        UserDataHeader header = UserDataHeader.Read(stream);

        //Assert
        Assert.IsNull(header, "Header should be null for invalid header length.");
    }

    [TestMethod]
    public void TestReadInvalidDataType()
    {
        //Arrange
        UInt32 headerLength = 18;
        UInt32 headerVersion = UserDataHeader.XVD_USER_DATA_VERSION;
        UInt32 dataLength = 2;
        UserDataType dataType = (UserDataType)999; //invalid data type

        //Act
        Stream stream = WriteTestStream(headerLength, headerVersion, dataType, dataLength, new byte[] { 1, 2 });
        UserDataHeader header = UserDataHeader.Read(stream);

        //Assert
        Assert.IsNull(header, "Header should be null for invalid header version.");
    }

    [TestMethod]

    public void TestReadWithExtraData()
    {
        //Arrange
        uint headerLength = 24; // 16 + 8 for extra data
        uint headerVersion = UserDataHeader.XVD_USER_DATA_VERSION;
        uint dataLength = 16;
        UserDataType dataType = UserDataType.XvdUserDataPackageFiles;
        byte[] extraData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        //Act
        Stream stream = WriteTestStream(headerLength, headerVersion, dataType, dataLength, extraData);
        UserDataHeader header = UserDataHeader.Read(stream);
        //Assert
        Assert.IsNotNull(header, "Header should not be null for valid data with extra data.");
        Assert.AreEqual(headerLength, header.HeaderLength, "Header length does not match expected value.");
        Assert.AreEqual(headerVersion, header.HeaderVersion, "Header version does not match expected value.");
        Assert.AreEqual(dataType, header.DataType, "Data Type does not match expected value. ");
    }
    
}
