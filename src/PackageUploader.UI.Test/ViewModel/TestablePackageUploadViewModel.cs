// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.Test.ViewModel
{
    /// <summary>
    /// A testable version of PackageUploadViewModel that allows overriding certain methods
    /// that would normally interact with the file system or external components.
    /// </summary>
    public class TestablePackageUploadViewModel : PackageUploadViewModel
    {
        // Flags to track method calls for test verification
        public bool ExtractIdInformationCalled { get; private set; }
        public bool GetBuildAndKeyIdCalled { get; private set; }
        public bool ExtractFileCalled { get; private set; }

        // Values to be returned by the overridden methods
        public Guid BuildIdToReturn { get; set; } = Guid.NewGuid();
        public Guid KeyIdToReturn { get; set; } = Guid.NewGuid();
        public string TypeToReturn { get; set; } = "MSIXVC";
        public string TitleIdToReturn { get; set; } = "ABCDEF12";
        public string StoreIdToReturn { get; set; } = "9NBLGGH42THS";
        public string LogoFilenameToReturn { get; set; } = "Assets/Logo.png";
        public byte[] FileContentsToReturn { get; set; } = null;
        public bool ThrowExceptionInExtractId { get; set; } = false;
        public bool ThrowExceptionInGetBuildAndKey { get; set; } = false;
        public Exception ExceptionToThrow { get; set; } = new FileNotFoundException("Test exception");
        public long FileSizeToReturn { get; set; } = 1024 * 1024 * 10; // 10 MB by default

        public TestablePackageUploadViewModel(
            PackageModelProvider packageModelProvider,
            IPackageUploaderService uploaderService,
            IWindowService windowService,
            UploadingProgressPercentageProvider uploadingProgressPercentageProvider,
            ErrorModelProvider errorModelProvider)
            : base(packageModelProvider, uploaderService, windowService, uploadingProgressPercentageProvider, errorModelProvider)
        {
        }

        /// <summary>
        /// Expose the protected ExtractPackageInformation method for testing
        /// </summary>
        public void TestExtractPackageInformation(string packagePath)
        {
            // Use reflection to access and invoke the private method
            MethodInfo extractPackageInfoMethod = typeof(PackageUploadViewModel)
                .GetMethod("ExtractPackageInformation", BindingFlags.NonPublic | BindingFlags.Instance);
            
            extractPackageInfoMethod?.Invoke(this, [packagePath]);
        }

        /// <summary>
        /// Override the XvcFile.GetBuildAndKeyId static method behavior
        /// </summary>
        protected override void GetBuildAndKeyId(string packagePath, out Guid buildId, out Guid keyId)
        {
            GetBuildAndKeyIdCalled = true;
            
            if (ThrowExceptionInGetBuildAndKey)
            {
                throw ExceptionToThrow;
            }
            
            buildId = BuildIdToReturn;
            keyId = KeyIdToReturn;
        }

        /// <summary>
        /// Override the ExtractIdInformationFromValidatorLog method
        /// </summary>
        protected override void ExtractIdInformationFromValidatorLog(
            Guid expectedBuildId, out string type, out string titleId, out string storeId, out string logoFilename)
        {
            ExtractIdInformationCalled = true;
            
            if (ThrowExceptionInExtractId)
            {
                throw ExceptionToThrow;
            }
            
            type = TypeToReturn;
            titleId = TitleIdToReturn;
            storeId = StoreIdToReturn;
            logoFilename = LogoFilenameToReturn;
        }

        /// <summary>
        /// Override the XvcFile.ExtractFile static method behavior
        /// </summary>
        protected override void ExtractFile(string packagePath, string fileName, out byte[] fileContents)
        {
            ExtractFileCalled = true;
            fileContents = FileContentsToReturn;
            
            // Directly set the PackagePreviewImage property for testing
            if (fileContents != null)
            {
                BitmapImage mockImage = new();
                
                // In tests, we bypass the actual image loading process
                // Instead, we just create an empty BitmapImage instance for the test to pass
                PackagePreviewImage = mockImage;
            }
        }

        /// <summary>
        /// Override file existence check for SymbolBundleFilePath
        /// </summary>
        protected override bool FileExists(string path)
        {
            // Return false for "NONEXISTENT" symbol files to properly test the empty symbol path case
            if (path.Contains("NONEXISTENT"))
            {
                return false;
            }
            
            // Return true for normal symbol files with our expected naming pattern
            if (path.Contains(TitleIdToReturn + ".zip"))
            {
                return true;
            }
            
            return File.Exists(path);
        }

        /// <summary>
        /// Override FileInfo for file size determination
        /// </summary>
        protected override long GetFileSize(string path)
        {
            return FileSizeToReturn;
        }
    }
}