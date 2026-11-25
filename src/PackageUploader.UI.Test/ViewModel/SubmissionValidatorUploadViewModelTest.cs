// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Moq;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Tools;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.Reflection;

namespace PackageUploader.UI.Test.ViewModel;

[TestClass]
public class SubmissionValidatorUploadViewModelTest
{
    private string _directory;
    private Mock<IPackageUploaderService> _uploader;
    private ValidatorViewModel _viewModel;
    private GameProduct _product;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"ValidatorSelection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _uploader = new Mock<IPackageUploaderService>(MockBehavior.Strict);
        _product = new GameProduct();
        var branch = Mock.Of<IGamePackageBranch>(value =>
            value.Name == "validator-test" && value.BranchType == GamePackageBranchType.Branch);
        var market = new GameMarketGroupPackage();
        typeof(GameMarketGroupPackage).GetProperty(nameof(market.Name))!.SetValue(market, "default");
        var configuration = new GamePackageConfiguration();
        typeof(GamePackageConfiguration).GetProperty(nameof(configuration.MarketGroupPackages))!
            .SetValue(configuration, new List<GameMarketGroupPackage> { market });
        _uploader.Setup(client => client.GetProductByBigIdAsync("9NBLGGH12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);
        _uploader.Setup(client => client.GetPackageBranchesAsync(_product, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { branch });
        _uploader.Setup(client => client.GetPackageConfigurationAsync(
                _product, branch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);
        _viewModel = new ValidatorViewModel(
            new PackageModelProvider { Package = new PackageModel { PackageFilePath = "not-selected" } },
            _uploader.Object);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    [DataRow(".xvc", "XVC")]
    [DataRow(".msixvc", "MSIXVC")]
    public void SelectingAndReopeningPackage_PreservesValidator(string extension, string type)
    {
        string packagePath = CreatePackage("valid", extension, type);
        _viewModel.PackageFilePath = packagePath;

        Assert.IsTrue(_viewModel.UploadPackageCommand.CanExecute(null));
        Assert.AreEqual(Path.Combine(_directory, "Validator_valid.xml"), _viewModel.SubValFilePath);
        _viewModel.OnAppearing();
        Assert.IsTrue(_viewModel.UploadPackageCommand.CanExecute(null));
        Assert.AreEqual(Path.Combine(_directory, "Validator_valid.xml"), _viewModel.SubValFilePath);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void EmptyValidatorPath_BlocksUpload(string path)
    {
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        Assert.IsTrue(_viewModel.UploadPackageCommand.CanExecute(null));

        _viewModel.SubValFilePath = path;

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
        _viewModel.UploadPackageCommand.Execute(null);
    }

    [TestMethod]
    public void ValidatorRemovedAfterSelection_BlocksUpload()
    {
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        Assert.IsTrue(_viewModel.UploadPackageCommand.CanExecute(null));
        File.Delete(_viewModel.SubValFilePath);

        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ValidatorUnavailableAtClick_ShowsErrorBeforeStartingUpload(bool removed)
    {
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        if (removed)
            File.Delete(_viewModel.SubValFilePath);
        else
            _viewModel.SubValFilePath = string.Empty;

        typeof(PackageUploadViewModel)
            .GetMethod("UploadPackageProcessAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_viewModel, null);

        Assert.AreEqual(string.Format(Resources.Strings.PackageUpload.SubValLogNotFoundErrMsg,
            Path.Combine(_directory, "Validator_valid.xml")), _viewModel.PackageErrorMessage);
        Assert.IsFalse(_viewModel.IsUploadInProgress);
        Assert.IsFalse(_uploader.Invocations.Any(call => call.Method.Name == nameof(IPackageUploaderService.UploadGamePackageAsync)));
    }

    [TestMethod]
    public async Task EarlierProductLookupCompletesAfterInvalidSelection_DoesNotEnableValidatorlessUpload()
    {
        var productLookup = new TaskCompletionSource<GameProduct>();
        _uploader.Setup(client => client.GetProductByBigIdAsync("9NBLGGH12345", It.IsAny<CancellationToken>()))
            .Returns(productLookup.Task);
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        Assert.IsTrue(_viewModel.IsLoadingBranchesAndFlights);

        _viewModel.PackageFilePath = CreatePackage("unreadable", ".xvc", "XVC");
        Assert.AreEqual(string.Empty, _viewModel.SubValFilePath);
        productLookup.SetResult(_product);
        await productLookup.Task;

        Assert.AreEqual(string.Empty, _viewModel.ProductName);
        Assert.AreEqual(0, _viewModel.BranchAndFlightNames.Length);
        _uploader.Verify(client => client.GetPackageBranchesAsync(
            It.IsAny<GameProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null),
            "A stale product response must not make an empty-validator selection uploadable.");
    }

    [TestMethod]
    public async Task EarlierBranchLookupCompletesAfterInvalidSelection_DoesNotRestoreProduct()
    {
        var branchLookup = new TaskCompletionSource<IReadOnlyCollection<IGamePackageBranch>>();
        _uploader.Setup(client => client.GetPackageBranchesAsync(_product, It.IsAny<CancellationToken>()))
            .Returns(branchLookup.Task);
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        Assert.IsTrue(_viewModel.IsLoadingBranchesAndFlights);

        _viewModel.PackageFilePath = CreatePackage("unreadable", ".xvc", "XVC");
        branchLookup.SetResult(new[] { Mock.Of<IGamePackageBranch>() });
        await branchLookup.Task;

        Assert.AreEqual(string.Empty, _viewModel.ProductName);
        Assert.AreEqual(0, _viewModel.BranchAndFlightNames.Length);
        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task EarlierLookupFailure_DoesNotClearNewerLoadingState()
    {
        var earlierLookup = new TaskCompletionSource<GameProduct>();
        var currentLookup = new TaskCompletionSource<GameProduct>();
        _uploader.Setup(client => client.GetProductByBigIdAsync("9NBLGGH12345", It.IsAny<CancellationToken>()))
            .Returns(earlierLookup.Task);
        _uploader.Setup(client => client.GetProductByBigIdAsync("9NBLGGH67890", It.IsAny<CancellationToken>()))
            .Returns(currentLookup.Task);
        _viewModel.PackageFilePath = CreatePackage("valid", ".xvc", "XVC");
        _viewModel.BigId = "9NBLGGH67890";

        earlierLookup.SetException(new IOException("Simulated superseded lookup failure."));
        await Assert.ThrowsExactlyAsync<IOException>(() => earlierLookup.Task);

        Assert.IsTrue(_viewModel.IsLoadingBranchesAndFlights);
        Assert.AreEqual(string.Empty, _viewModel.BranchOrFlightErrorMessage);
        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));

        currentLookup.SetResult(_product);
        await currentLookup.Task;
        Assert.IsFalse(_viewModel.IsLoadingBranchesAndFlights);
        Assert.IsTrue(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    [TestMethod]
    public void MissingValidator_ManualStoreIdDoesNotHideErrorOrEnableUpload()
    {
        string packagePath = CreatePackage("valid", ".xvc", "XVC");
        string validatorPath = Path.Combine(_directory, "Validator_valid.xml");
        File.Delete(validatorPath);
        _viewModel.PackageFilePath = packagePath;

        _viewModel.BigId = "9NBLGGH12345";

        StringAssert.Contains(_viewModel.PackageErrorMessage,
            string.Format(Resources.Strings.PackageUpload.SubValLogNotFoundErrMsg, validatorPath));
        Assert.IsFalse(_viewModel.UploadPackageCommand.CanExecute(null));
    }

    private string CreatePackage(string name, string extension, string type)
    {
        string path = Path.Combine(_directory, name + extension);
        File.WriteAllBytes(path, new byte[8192]);
        File.WriteAllText(Path.Combine(_directory, $"Validator_{name}.xml"), $"""
            <SubmissionValidator>
              <project>
                <BuildId>{ValidatorViewModel.BuildId}</BuildId>
                <Type>{type}</Type>
              </project>
              <GameConfig>
                <Game><StoreId>9NBLGGH12345</StoreId></Game>
              </GameConfig>
            </SubmissionValidator>
            """);
        return path;
    }

    private sealed class ValidatorViewModel(PackageModelProvider package, IPackageUploaderService uploader)
        : PackageUploadViewModel(package, uploader, Mock.Of<IWindowService>(),
            new UploadingProgressPercentageProvider(), new ErrorModelProvider(),
            new PathConfigurationProvider(), new Msixvc2ToolResolver())
    {
        internal static readonly Guid BuildId = Guid.Parse("01000000-0000-0000-0000-000000000001");

        protected override void GetBuildAndKeyId(string packagePath, out Guid buildId, out Guid keyId)
        {
            if (Path.GetFileNameWithoutExtension(packagePath) == "unreadable")
                throw new InvalidDataException("Cannot read package metadata.");
            buildId = BuildId;
            keyId = Guid.Empty;
        }
    }
}
