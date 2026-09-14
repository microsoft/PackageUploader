// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace PackageUploader.UI.Test.Model
{
    [TestClass]
    public class Msixvc2LogoExtractorTest
    {
        /// <summary>
        /// A verbatim "packageutil fileinfo" listing, so the parser is pinned to the real output
        /// format rather than to an idealised version of it. The rows with a blank chunk column are
        /// package user data, which cannot be extracted.
        /// </summary>
        private const string RealListing =
            "packageutil version 2610.308.24000.0\r\n" +
            "\r\n" +
            "Chunk Name                                                     Size       \r\n" +
            "───── ──────────────────────────────────────────────────────── ────────── \r\n" +
            "      MicrosoftGame.config                                                \r\n" +
            "      AppxManifest.xml                                                    \r\n" +
            "REG   MicrosoftGame.config                                     1.041 KB   \r\n" +
            "REG   SplashScreen.png                                         15.253 KB  \r\n" +
            "REG   Square150x150Logo.png                                    18.954 KB  \r\n" +
            "REG   StoreLogo.png                                            4.372 KB   \r\n" +
            "1000  Square310x310Logo.png                                    66.044 KB  \r\n" +
            "1001  dir_00000\\file_00000000.bin                              51.200 MB  \r\n" +
            "\r\n";

        [TestMethod]
        public void ParseFileNames_ReturnsPayloadFilesInListedOrder()
        {
            var names = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            CollectionAssert.AreEqual(
                new[]
                {
                    "MicrosoftGame.config",
                    "SplashScreen.png",
                    "Square150x150Logo.png",
                    "StoreLogo.png",
                    "Square310x310Logo.png",
                    @"dir_00000\file_00000000.bin",
                },
                (System.Collections.ICollection)names,
                "Only the rows with a chunk are extractable payload files.");
        }

        [TestMethod]
        public void ParseFileNames_ReturnsEmptyForOutputWithoutATable()
        {
            Assert.AreEqual(0, Msixvc2LogoExtractor.ParseFileNames("packageutil version 1.0\r\n\r\nerror: bad package\r\n").Count);
            Assert.AreEqual(0, Msixvc2LogoExtractor.ParseFileNames(string.Empty).Count);
            Assert.AreEqual(0, Msixvc2LogoExtractor.ParseFileNames(null!).Count);
        }

        [TestMethod]
        public void SelectAsset_MatchesCaseInsensitivelyAndReturnsThePackagedSpelling()
        {
            var packaged = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            // MicrosoftGame.config declares its assets in a casing that need not match the packaged
            // files, and packageutil's /file match is exact, so the packaged spelling must come back.
            var selected = Msixvc2LogoExtractor.SelectAsset(packaged, ["square150x150logo.png"]);

            Assert.AreEqual("Square150x150Logo.png", selected);
        }

        [TestMethod]
        public void SelectAsset_PrefersTheFullRelativePathOverABareNameMatch()
        {
            // Two logos with the same name in different directories: only the path tells them apart.
            string[] packaged =
            [
                @"Assets\en-us\Logo.png",
                @"Assets\ja-jp\Logo.png",
            ];

            Assert.AreEqual(@"Assets\ja-jp\Logo.png", Msixvc2LogoExtractor.SelectAsset(packaged, [@"Assets\ja-jp\Logo.png"]));
            Assert.AreEqual(@"Assets\en-us\Logo.png", Msixvc2LogoExtractor.SelectAsset(packaged, [@"Assets\en-us\Logo.png"]));
        }

        [TestMethod]
        public void SelectAsset_SkipsAnAmbiguousNameRatherThanGuessing()
        {
            string[] packaged =
            [
                @"Assets\en-us\Logo.png",
                @"Assets\ja-jp\Logo.png",
                @"Assets\StoreLogo.png",
            ];

            // The requested path is not in the package and the bare name is ambiguous, so the wrong
            // logo must not be returned. The next candidate, which is unambiguous, is used instead.
            var selected = Msixvc2LogoExtractor.SelectAsset(packaged, [@"Assets\fr-fr\Logo.png", "StoreLogo.png"]);

            Assert.AreEqual(@"Assets\StoreLogo.png", selected);
        }

        [TestMethod]
        public void SelectAsset_FallsBackToAnUnambiguousNameWhenPackagingRelocatedTheAsset()
        {
            // Generated tiles are emitted at the package root even when the source asset lived in a
            // subdirectory, so an unambiguous name still has to match.
            var packaged = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            var selected = Msixvc2LogoExtractor.SelectAsset(packaged, [@"Assets\Square150x150Logo.png"]);

            Assert.AreEqual("Square150x150Logo.png", selected);
        }

        [TestMethod]
        public void SelectAsset_TreatsForwardAndBackSlashesAsEquivalent()
        {
            string[] packaged = [@"Assets\en-us\Logo.png"];

            Assert.AreEqual(@"Assets\en-us\Logo.png", Msixvc2LogoExtractor.SelectAsset(packaged, ["Assets/en-us/Logo.png"]));
        }

        [TestMethod]
        public void GetRelativeAssetPath_RecoversThePathDeclaredInTheConfig()
        {
            string configDirectory = @"C:\temp\layout";

            Assert.AreEqual(@"Assets\Logo.png", Msixvc2LogoExtractor.GetRelativeAssetPath(configDirectory, @"C:\temp\layout\Assets\Logo.png"));
            Assert.AreEqual("Logo.png", Msixvc2LogoExtractor.GetRelativeAssetPath(configDirectory, @"C:\temp\layout\Logo.png"));
        }

        [TestMethod]
        public void GetRelativeAssetPath_FallsBackToTheNameForAssetsOutsideTheConfigDirectory()
        {
            // A config holding an absolute path resolves outside its own directory; only the name
            // is usable then.
            Assert.AreEqual("Logo.png", Msixvc2LogoExtractor.GetRelativeAssetPath(@"C:\temp\layout", @"D:\elsewhere\Logo.png"));
            Assert.AreEqual("Logo.png", Msixvc2LogoExtractor.GetRelativeAssetPath(@"C:\temp\layout", @"C:\temp\Logo.png"));
        }

        [TestMethod]
        public void GetRelativeAssetPath_HandlesEmptyInput()
        {
            Assert.AreEqual(string.Empty, Msixvc2LogoExtractor.GetRelativeAssetPath(@"C:\temp", string.Empty));
            Assert.AreEqual(@"C:\temp\Logo.png", Msixvc2LogoExtractor.GetRelativeAssetPath(string.Empty, @"C:\temp\Logo.png"));
        }

        [TestMethod]
        public void SelectAsset_StillMatchesAnAbsolutePathByName()
        {
            var packaged = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            // GetRelativeAssetPath normally reduces these to a relative path, but an asset that sits
            // outside the config's directory stays absolute, and the name must still resolve it.
            var selected = Msixvc2LogoExtractor.SelectAsset(packaged, [@"C:\temp\layout\StoreLogo.png"]);

            Assert.AreEqual("StoreLogo.png", selected);
        }

        [TestMethod]
        public void SelectAsset_PrefersTheEarlierCandidateAndSkipsEmptyOnes()
        {
            var packaged = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            var selected = Msixvc2LogoExtractor.SelectAsset(
                packaged,
                ["", "   ", "NotInPackage.png", "StoreLogo.png", "Square150x150Logo.png"]);

            Assert.AreEqual("StoreLogo.png", selected, "The first candidate the package actually contains wins.");
        }

        [TestMethod]
        public void SelectAsset_ReturnsNullWhenNothingMatches()
        {
            var packaged = Msixvc2LogoExtractor.ParseFileNames(RealListing);

            Assert.IsNull(Msixvc2LogoExtractor.SelectAsset(packaged, ["Missing.png"]));
            Assert.IsNull(Msixvc2LogoExtractor.SelectAsset(new List<string>(), ["StoreLogo.png"]));
        }

        [TestMethod]
        public void TryExtractAsset_ReturnsNullWhenPackageUtilIsUnavailable()
        {
            string packagePath = Path.GetTempFileName();

            try
            {
                Assert.IsNull(Msixvc2LogoExtractor.TryExtractAsset(string.Empty, packagePath, ["StoreLogo.png"]));
                Assert.IsNull(Msixvc2LogoExtractor.TryExtractAsset(@"C:\nonexistent\packageutil.exe", packagePath, ["StoreLogo.png"]));
            }
            finally
            {
                File.Delete(packagePath);
            }
        }

        [TestMethod]
        public void TryExtractAsset_ReturnsNullWhenThePackageIsMissing()
        {
            string packageUtilPath = Path.GetTempFileName();

            try
            {
                Assert.IsNull(Msixvc2LogoExtractor.TryExtractAsset(
                    packageUtilPath,
                    Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msixvc"),
                    ["StoreLogo.png"]));
            }
            finally
            {
                File.Delete(packageUtilPath);
            }
        }
    }
}
