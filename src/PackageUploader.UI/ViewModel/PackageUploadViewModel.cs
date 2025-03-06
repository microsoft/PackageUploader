// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using PackageUploader.UI.Model;
using PackageUploader.UI.Services;

namespace PackageUploader.UI.ViewModel;

public partial class PackageUploadViewModel : BaseViewModel
{
    private readonly PackageModelService _packageModelService;
    private readonly PathConfigurationService _pathConfigurationService;

    private bool _isSpinnerRunning = false;
    public bool IsSpinnerRunning
    {
        get => _isSpinnerRunning;
        set => SetProperty(ref _isSpinnerRunning, value);
    }

    private string _branchFriendlyName = "Main";
    public string BranchFriendlyName
    {
        get => _branchFriendlyName;
        set => SetProperty(ref _branchFriendlyName, value);
    }

    private string _marketGroupName = "default";
    public string MarketGroupName
    {
        get => _marketGroupName;
        set => SetProperty(ref _marketGroupName, value);
    }

    private string[] _branchFriendlyNames = ["Main"];
    public string[] BranchFriendlyNames
    {
        get => _branchFriendlyNames;
        set
        {
            if (SetProperty(ref _branchFriendlyNames, value))
            {
                OnPropertyChanged(nameof(BranchFriendlyName));
            }
        }
    }

    private string _flightName = string.Empty;
    public string FlightName
    {
        get => _flightName;
        set => SetProperty(ref _flightName, value);
    }

    private string[] _flightNames = [];
    public string[] FlightNames
    {
        get => _flightNames;
        set
        {
            if (SetProperty(ref _flightNames, value))
            {
                OnPropertyChanged(nameof(FlightName));
            }
        }
    }
    
    public PackageModel Package => _packageModelService.Package;
    
    // Package properties accessed from the shared service
    public string BigId 
    { 
        get => Package.BigId;
        set
        {
            if (Package.BigId != value)
            {
                Package.BigId = value;
                OnPropertyChanged();
            }
        }
    }

    public string PackageFilePath 
    { 
        get => Package.PackageFilePath;
        set
        {
            if (Package.PackageFilePath != value)
            {
                Package.PackageFilePath = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string EkbFilePath
    {
        get => Package.EkbFilePath;
        set
        {
            if (Package.EkbFilePath != value)
            {
                Package.EkbFilePath = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string SubValFilePath
    {
        get => Package.SubValFilePath;
        set
        {
            if (Package.SubValFilePath != value)
            {
                Package.SubValFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isPackageUploadEnabled = true;
    public bool IsPackageUploadEnabled
    {
        get => _isPackageUploadEnabled;
        set => SetProperty(ref _isPackageUploadEnabled, value);
    }

    private string _packageUploadTooltip = string.Empty;
    public string PackageUploadTooltip
    {
        get => _packageUploadTooltip;
        set => SetProperty(ref _packageUploadTooltip, value);
    }

    public ICommand UploadPackageCommand { get; }

    private readonly string ConfileFilePath = Path.GetTempFileName();
    private readonly ArrayList processOutput = [];
    private Process? packageUploaderProcess;

    public PackageUploadViewModel(PackageModelService packageModelService, PathConfigurationService pathConfigurationService)
    {
        _packageModelService = packageModelService;
        _pathConfigurationService = pathConfigurationService;

        if (File.Exists(_pathConfigurationService.PackageUploaderPath))
        {
            IsPackageUploadEnabled = true;
        }
        else
        {
            IsPackageUploadEnabled = false;
            PackageUploadTooltip = "PackageUploader.exe was not found. Package upload is unavailable.";
        }

        UploadPackageCommand = new Command(StartUploadPackageProcess);

        StartGetProductInfo();
    }

    private void StartGetProductInfo()
    {
        var config = new GetProductConfig
        {
            bigId = BigId
        };
        string configFileText = System.Text.Json.JsonSerializer.Serialize(config);
        File.WriteAllText(ConfileFilePath, configFileText);

        string pkgBuildExePath = _pathConfigurationService.PackageUploaderPath;
        string cmdFormat = $"GetProduct -c {ConfileFilePath} -a default";

        packageUploaderProcess = new Process();
        packageUploaderProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(pkgBuildExePath);
        packageUploaderProcess.StartInfo.FileName = pkgBuildExePath;
        packageUploaderProcess.StartInfo.Arguments = cmdFormat;
        packageUploaderProcess.StartInfo.RedirectStandardOutput = true;
        packageUploaderProcess.StartInfo.RedirectStandardError = true;
        packageUploaderProcess.EnableRaisingEvents = true;
        packageUploaderProcess.StartInfo.CreateNoWindow = true;

        packageUploaderProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        packageUploaderProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        packageUploaderProcess.Exited += (sender, args) =>
        {
            IsSpinnerRunning = false;
            string outputString = string.Join("\n", processOutput.ToArray());

            // Parse Get Product Output
            ProcessGetProductOutput(outputString);
            packageUploaderProcess.WaitForExit();
        };

        packageUploaderProcess.Start();
        packageUploaderProcess.BeginOutputReadLine();
        packageUploaderProcess.BeginErrorReadLine();
        IsSpinnerRunning = true;
    }

    [GeneratedRegex(@"PackageUploader\.Application\.Operations\.GetProductOperation\[0\] Product: \{(?<ProductResponse>.*?)\}")]
    private static partial Regex ProductResponseRegex();

    private void ProcessGetProductOutput(string outputString)
    {
        if (outputString == null)
        {
            return;
        }

        string? productResponseString = null;
        MatchCollection productResponseCollection = ProductResponseRegex().Matches(outputString);

        for (int i = 0; i < productResponseCollection.Count; i++)
        {
            productResponseString = "{" + productResponseCollection[i].Groups["ProductResponse"].Value + "}";
            if (productResponseString != null)
            {
                break;
            }
        }

        if (productResponseString == null)
        {
            return;
        }

        GetProductResponse? response = System.Text.Json.JsonSerializer.Deserialize<GetProductResponse>(productResponseString);

        if (response != null)
        {
            BranchFriendlyNames = response.branchFriendlyNames;
            FlightNames = response.flightNames;
        }
    }

    private void StartUploadPackageProcess()
    {
        IsSpinnerRunning = true;

        var config = new UploadConfig
        {
            bigId = BigId,
            branchFriendlyName = BranchFriendlyName,
            flightName = FlightName,
            packageFilePath = PackageFilePath,
            marketGroupName = MarketGroupName,
            gameAssets = new GameAssets
            {
                ekbFilePath = EkbFilePath,
                subValFilePath = SubValFilePath
            }
        };
        SemanticScreenReader.Announce("Config created");

        string configFileText = System.Text.Json.JsonSerializer.Serialize(config);
        File.WriteAllText(ConfileFilePath, configFileText);
        //SubHeadLine.Text = $"Config file created at {ConfileFilePath}";

        string pkgBuildExePath = _pathConfigurationService.PackageUploaderPath;
        string cmdFormat = $"UploadXvcPackage -c {ConfileFilePath} -a default";
        packageUploaderProcess = new Process();
        packageUploaderProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(pkgBuildExePath);
        packageUploaderProcess.StartInfo.FileName = pkgBuildExePath;
        packageUploaderProcess.StartInfo.Arguments = cmdFormat;
        packageUploaderProcess.StartInfo.RedirectStandardOutput = true;
        packageUploaderProcess.StartInfo.RedirectStandardError = true;
        packageUploaderProcess.EnableRaisingEvents = true;
        packageUploaderProcess.StartInfo.CreateNoWindow = true;
        packageUploaderProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data)) // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.outputdatareceived?view=net-9.0
            {
                processOutput.Add(args.Data);
            }
        };
        packageUploaderProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };
        packageUploaderProcess.Exited += (sender, args) =>
        {
            string outputString = string.Join("", processOutput.ToArray());
            packageUploaderProcess.WaitForExit();

            // After upload process completes
            IsSpinnerRunning = false;
        };
        packageUploaderProcess.Start();
        packageUploaderProcess.BeginOutputReadLine();
        packageUploaderProcess.BeginErrorReadLine();
    }
}
