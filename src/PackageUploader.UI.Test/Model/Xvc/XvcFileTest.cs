namespace PackageUploader.UI.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.IO;
using System.Text;

[TestClass]
public class XvcFileTest
{
    private XvcFile _xvcFile;
    private string _pkgPath;
    private Guid _knownBuildId;
    private Guid _knownKeyId;

    [TestInitialize]
    public void Setup()
    {
        _pkgPath = Path.Combine(Path.GetTempPath(), "TestXvcFile.xvc");
        _knownBuildId = Guid.NewGuid();
        _knownKeyId = Guid.NewGuid();

        HeaderTestBinaryFiller(_pkgPath, _knownBuildId, _knownKeyId);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_pkgPath))
        {
            File.Delete(_pkgPath);
        }
    }

    [TestMethod]
    public void BytesToPagesTest()
    {
        // Arrange
        UInt64 _bytes = 0;
        
        // Act & Assert
        Assert.AreEqual((UInt32)0, XvcFile.BytesToPages(_bytes), "BytesToPages should return 0 for 0 bytes");

        // Arrange
        _bytes = 4096;
        
        // Act & Assert
        Assert.AreEqual((UInt32)1, XvcFile.BytesToPages(_bytes), "BytesToPages should return 1 for 4096 bytes");

        // Arrange
        _bytes = 8192;
        
        // Act & Assert
        Assert.AreEqual((UInt32)2, XvcFile.BytesToPages(_bytes), "BytesToPages should return 2 for 8192 bytes");

        // Arrange
        _bytes = (UInt64)new Random().NextInt64();
        
        // Act & Assert
        Assert.AreEqual((UInt32)(_bytes / 4096), XvcFile.BytesToPages(_bytes), "BytesToPages should return bytes / 4096");
    }

    [TestMethod]
    public void PagesToBytesTest()
    {
        // Arrange
        UInt32 _pages = 0;
        
        // Act & Assert
        Assert.AreEqual((UInt64)0, XvcFile.PagesToBytes(_pages), "PagesToBytes should return 0 for 0 pages");

        // Arrange
        _pages = 1;
        
        // Act & Assert
        Assert.AreEqual((UInt64)4096, XvcFile.PagesToBytes(_pages), "PagesToBytes should return 4096 for 1 page");

        // Arrange
        _pages = 2;
        
        // Act & Assert
        Assert.AreEqual((UInt64)8192, XvcFile.PagesToBytes(_pages), "PagesToBytes should return 8192 for 2 pages");

        // Arrange
        _pages = (UInt32)new Random().Next(1, 1000);
        
        // Act & Assert
        Assert.AreEqual((UInt64)_pages * 4096, XvcFile.PagesToBytes(_pages), "PagesToBytes should return pages * 4096");
    }

    [TestMethod]
    public void GetBuildAndKeyIdTest()
    {
        // Arrange
        Guid _foundBuildId;
        Guid _foundKeyId;
        
        // Act
        XvcFile.GetBuildAndKeyId(_pkgPath, out _foundBuildId, out _foundKeyId);

        // Assert
        Assert.AreEqual(_knownBuildId, _foundBuildId, "The found build ID does not match the known build ID.");
        Assert.AreEqual(_knownKeyId, _foundKeyId, "The found key ID does not match the known key ID.");
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void GetBuildAndKeyId_ThrowsExceptionForInvalidPath()
    {
        // Arrange
        string invalidPath = Path.Combine(Path.GetTempPath(), "NonexistentFile.xvc");
        Guid buildId;
        Guid keyId;
        
        // Act - should throw FileNotFoundException
        XvcFile.GetBuildAndKeyId(invalidPath, out buildId, out keyId);
    }

    [TestMethod]
    public void GetPackageFileSize_ReturnsZeroForNullUserDataFiles()
    {
        // Act
        uint size = XvcFile.GetPackageFileSize(null, "test.txt");
        
        // Assert
        Assert.AreEqual(0u, size, "GetPackageFileSize should return 0 for null userDataFiles");
    }

    [TestMethod]
    public void GetPackageFileSize_ReturnsZeroForNonExistentFile()
    {
        // Arrange
        var userDataFiles = new UserDataPackageFile[]
        {
            new UserDataPackageFile { FilePath = "file1.txt", FileSize = 100, Offset = 0 },
            new UserDataPackageFile { FilePath = "file2.txt", FileSize = 200, Offset = 100 }
        };
        
        // Act
        uint size = XvcFile.GetPackageFileSize(userDataFiles, "nonexistent.txt");
        
        // Assert
        Assert.AreEqual(0u, size, "GetPackageFileSize should return 0 for non-existent file");
    }

    [TestMethod]
    public void GetPackageFileSize_ReturnsCorrectSize()
    {
        // Arrange
        string testFilePath = "test.txt";
        uint expectedSize = 123;
        var userDataFiles = new UserDataPackageFile[]
        {
            new UserDataPackageFile { FilePath = "file1.txt", FileSize = 100, Offset = 0 },
            new UserDataPackageFile { FilePath = testFilePath, FileSize = expectedSize, Offset = 100 }
        };
        
        // Act
        uint actualSize = XvcFile.GetPackageFileSize(userDataFiles, testFilePath);
        
        // Assert
        Assert.AreEqual(expectedSize, actualSize, "GetPackageFileSize should return the correct file size");
    }

    [TestMethod]
    public void ExtractFileTest()
    {
        // Arrange
        byte[] fileContents;
        
        // Act
        XvcFile.ExtractFile(_pkgPath, "nonexistent.txt", out fileContents);
        
        // Assert
        Assert.IsNull(fileContents, "ExtractFile should return null for non-existent files");
    }

    // Creates a Dummy Package Binary
    public static void HeaderTestBinaryFiller(string _pkgPath, Guid _knownBuildId, Guid _knownKeyId)
    {
        using (var stream = File.OpenWrite(_pkgPath))
        {
            using (var writer = new BinaryWriter(stream))
            {
                // Write the Binary for the Package
                byte[] _signature = new byte[XvdHeader.XVD_SIGNATURE_LENGTH];
                writer.Write(_signature);

                UInt64 _cookie = (UInt64)new Random().NextInt64();
                writer.Write(_cookie);

                UInt32 _featureBits = 1;
                writer.Write(_featureBits);

                UInt32 _formatVersion = 1;
                writer.Write(_formatVersion);

                UInt32 _creationTime = (UInt32)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                writer.Write(_creationTime);

                UInt64 _driveSize = (UInt64)new Random().NextInt64();
                writer.Write(_driveSize);

                Guid _vdUid = Guid.NewGuid();
                writer.Write(_vdUid.ToByteArray());

                Guid _uvUid = Guid.NewGuid();
                writer.Write(_uvUid.ToByteArray());

                byte[] _rootHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
                new Random().NextBytes(_rootHash);
                writer.Write(_rootHash);

                byte[] _xvcHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
                new Random().NextBytes(_xvcHash);
                writer.Write(_xvcHash);

                UInt32 _type = 1; // Example type
                writer.Write(_type);

                UInt32 _contentType = (UInt32)XvdContentType.DevPackage;
                writer.Write(_contentType);

                UInt32 _embeddedXvdLength = (UInt32)new Random().NextInt64();
                writer.Write(_embeddedXvdLength);

                UInt32 _userDataLength = (UInt32)new Random().NextInt64();
                writer.Write(_userDataLength);

                UInt32 _xvcLength = (UInt32)new Random().NextInt64();
                writer.Write(_xvcLength);

                UInt32 _dynHeaderLength = (UInt32)new Random().NextInt64();
                writer.Write(_dynHeaderLength);

                UInt32 _blockSize = 4096; // Example block size 
                writer.Write(_blockSize);

                // Ext Entry is a placeholder for additional data, typically used for extensions or metadata
                byte[] _extEntry = new byte[4 * (sizeof(UInt32) * 4 + sizeof(UInt64))];
                new Random().NextBytes(_extEntry);
                writer.Write(_extEntry);

                // Capabilities
                UInt16[] _capabilities = new UInt16[8];
                for (int i = 0; i < _capabilities.Length; i++)
                {
                    _capabilities[i] = (UInt16)new Random().Next(0, UInt16.MaxValue);
                }

                foreach (var cap in _capabilities)
                {
                    writer.Write(cap);
                }

                byte[] _peCatalogHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
                new Random().NextBytes(_peCatalogHash);
                writer.Write(_peCatalogHash);

                Guid _embeddedXvdPduid = Guid.NewGuid();
                writer.Write(_embeddedXvdPduid.ToByteArray());

                // Reserved0 is a placeholder for future use, typically reserved for alignment or future extensions
                byte[] _reserved0 = new byte[16];
                new Random().NextBytes(_reserved0);
                writer.Write(_reserved0);

                byte[] _keyMaterial = new byte[XvdHeader.XVD_KEY_LENGTH_BYTES];
                new Random().NextBytes(_keyMaterial);
                writer.Write(_keyMaterial);

                byte[] _userDataHash = new byte[XvdHeader.XVD_ROOT_HASH_LENGTH];
                new Random().NextBytes(_userDataHash);
                writer.Write(_userDataHash);

                byte[] _sandboxId = new byte[XvdHeader.XVD_SANDBOX_ID_LENGTH];
                new Random().NextBytes(_sandboxId);
                writer.Write(_sandboxId);

                Guid _productId = Guid.NewGuid();
                writer.Write(_productId.ToByteArray());

                Guid _pduid = _knownBuildId; // Use a known build ID for testing
                writer.Write(_pduid.ToByteArray());

                UInt64 _packageVersionNumber = (UInt64)new Random().NextInt64();
                writer.Write(_packageVersionNumber);

                byte[] _peCatalogCaps = new byte[XvdHeader.XVD_MAX_PE_CATALOGS * XvdHeader.XVD_MAX_PE_CATALOG_CAPS * sizeof(UInt16)];
                new Random().NextBytes(_peCatalogCaps);
                writer.Write(_peCatalogCaps);

                byte[] _peCatalogs = new byte[XvdHeader.XVD_MAX_PE_CATALOGS * XvdHeader.XVD_ROOT_HASH_LENGTH];
                new Random().NextBytes(_peCatalogs);
                writer.Write(_peCatalogs);

                UInt32 _writeableExpirationDate = (UInt32)new Random().NextInt64();
                writer.Write(_writeableExpirationDate);

                UInt32 _writeablePolicyFlags = (UInt32)new Random().NextInt64();
                writer.Write(_writeablePolicyFlags);

                UInt32 _plsSize = (UInt32)new Random().NextInt64();
                writer.Write(_plsSize);

                Byte _mutableXvcPageCount = (Byte)new Random().Next(0, Byte.MaxValue);
                writer.Write(_mutableXvcPageCount);

                Byte _platform = (Byte)new Random().Next(0, 255); // Example platform value
                writer.Write(_platform);

                writer.Write(new byte[26]); // UnusedBytes

                UInt64 _sequenceNumber = (UInt64)new Random().NextInt64();
                writer.Write(_sequenceNumber);

                UInt64 _minSysVer = (UInt64)new Random().NextInt64();
                writer.Write(_minSysVer);

                UInt32 _odkId = (UInt32)new Random().NextInt64();
                writer.Write(_odkId);

                // Maybe need to move stream position because xvcHeaderOffset

                // XvcHeader
                Guid _Id = Guid.NewGuid();
                writer.Write(_Id.ToByteArray());

                Guid[] _keyId = new Guid[XvcHeader.XVC_MAX_KEY_COUNT];
                _keyId[0] = _knownKeyId; // Use a known key ID for testing  
                foreach (var key in _keyId)
                {
                    writer.Write(key.ToByteArray());
                }

                string _description = "Test XVC Package Header";
                writer.Write(_description.PadRight(XvcHeader.XVC_MAX_DESCRIPTION_CHARS, '\0').ToCharArray());

                uint _version = 1; // Example version
                writer.Write(_version);

                uint _numberRegions = (uint)new Random().Next(1, 10); // Random number of regions
                writer.Write(_numberRegions);

                UInt32 _flags = (UInt32)(XvcHeaderFlags.XVC_HEADER_FLAG_XTS_OFFSETS_CACHED | XvcHeaderFlags.XVC_HEADER_FLAG_STRICT_REGION_MATCH);
                writer.Write(_flags);

                ushort _langId = 0x0409; // English (United States)
                writer.Write(_langId);

                uint _numberKeyIds = 1; // Only one key ID for this test
                writer.Write(_numberKeyIds);

                UInt32 _xvcType = 1; // Example type
                writer.Write(_xvcType);

                UInt32 _initialPlayRegionId = 0; // Example region ID
                writer.Write(_initialPlayRegionId);

                UInt64 _initialPlayOffset = 0; // Example offset
                writer.Write(_initialPlayOffset);

                UInt64 _creationTimeXvc = (UInt64)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                writer.Write(_creationTimeXvc);

                UInt32 _previewRegionId = 0; // Example preview region ID
                writer.Write(_previewRegionId);

                UInt32 _numberSegments = (UInt32)new Random().Next(1, 5); // Random number of segments
                writer.Write(_numberSegments);

                UInt64 _previewOffset = 0; // Example preview offset
                writer.Write(_previewOffset);

                UInt64 _unusedLength = (UInt64)new Random().NextInt64(); // Example unused length
                writer.Write(_unusedLength);

                UInt32 _numberRegionSpecifiers = (UInt32)new Random().Next(1, 5); // Random number of region specifiers
                writer.Write(_numberRegionSpecifiers);

                UInt32 _numberXtsEntries = (UInt32)new Random().Next(1, 5); // Random number of XTS entries
                writer.Write(_numberXtsEntries);

                writer.Write(new byte[sizeof(ulong) * 10]); // Reserved2
            }
        }
    }
}
