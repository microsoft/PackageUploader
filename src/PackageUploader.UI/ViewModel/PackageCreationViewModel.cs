// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using PackageUploader.UI.Model;
using PackageUploader.UI.View;
using PackageUploader.UI.Services;

namespace PackageUploader.UI.ViewModel;

public partial class PackageCreationViewModel : BaseViewModel
{
    private readonly PackageModelService _packageModelService;
    private readonly PathConfigurationService _pathConfigurationService;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            IsErrorVisible = !string.IsNullOrEmpty(value);
        }
    }

    private bool _isErrorVisible;
    public bool IsErrorVisible
    {
        get => _isErrorVisible;
        private set => SetProperty(ref _isErrorVisible, value);
    }

    private string _gameDataPath = string.Empty;
    public string GameDataPath
    {
        get => _gameDataPath;
        set => SetProperty(ref _gameDataPath, value);
    }

    private string _mappingDataXmlPath = string.Empty;
    public string MappingDataXmlPath
    {
        get => _mappingDataXmlPath;
        set => SetProperty(ref _mappingDataXmlPath, value);
    }

    private bool _isSpinnerRunning = false;
    public bool IsSpinnerRunning
    {
        get => _isSpinnerRunning;
        set => SetProperty(ref _isSpinnerRunning, value);
    }
    
    public PackageModel Package => _packageModelService.Package;

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

    private bool _isMakePkgEnabled = true;
    public bool IsMakePkgEnabled
    {
        get => _isMakePkgEnabled;
        set => SetProperty(ref _isMakePkgEnabled, value);
    }

    public ICommand MakePackageCommand { get; }
    
    public PackageCreationViewModel(PackageModelService packageModelService, PathConfigurationService pathConfigurationService)
    {
        _packageModelService = packageModelService;
        _pathConfigurationService = pathConfigurationService;

        if (File.Exists(_pathConfigurationService.MakePkgPath))
        {
            IsMakePkgEnabled = true;
        }
        else
        {
            IsMakePkgEnabled = false;
            ErrorMessage = "MakePkg.exe was not found. Please install the GDK in order to package game contents.";
        }

        MakePackageCommand = new Command(StartMakePackageProcess);
    }

    private async void StartMakePackageProcess()
    {
        // Reset package data
        _packageModelService.Package = new PackageModel();
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(GameDataPath))
        {
            ErrorMessage = "Please provide the game data path";
            return;
        }

        Process? makePackageProcess;
        ArrayList processOutput = [];
        string tempBuildPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempBuildPath);

        if (string.IsNullOrEmpty(MappingDataXmlPath))
        {
            // Need to generate our own mapping file
            await GenerateMappingFile(tempBuildPath);
        }

        // Return if we still don't have a mapping file.
        if (string.IsNullOrEmpty(MappingDataXmlPath))
        {
            return;
        }

        string cmdFormat = "pack /v /f {0} /d {1} /pd {2}";

        makePackageProcess = new Process();
        makePackageProcess.StartInfo.FileName = _pathConfigurationService.MakePkgPath;
        makePackageProcess.StartInfo.Arguments = string.Format(cmdFormat, MappingDataXmlPath, GameDataPath, tempBuildPath);
        makePackageProcess.StartInfo.RedirectStandardOutput = true;
        makePackageProcess.StartInfo.RedirectStandardError = true;
        makePackageProcess.EnableRaisingEvents = true;
        makePackageProcess.StartInfo.CreateNoWindow = true;
        
        makePackageProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };
        
        makePackageProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };
        
        makePackageProcess.Exited += (sender, args) =>
        {
            IsSpinnerRunning = false;
            string outputString = string.Join("", processOutput.ToArray());

            // Parse Make Package Output
            ProcessMakePackageOutput(outputString, tempBuildPath);
            makePackageProcess.WaitForExit();

            if (makePackageProcess.ExitCode != 0)
            {
                // Show error message
                ErrorMessage = "Error creating package.";
                return;
            }

            // Navigate on the main thread (This is temporary, probably will add another page here showing the package status and see if they want to upload).
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("///" + nameof(PackageUploadView));
            });
        };

        makePackageProcess.Start();
        makePackageProcess.BeginOutputReadLine();
        makePackageProcess.BeginErrorReadLine();
        IsSpinnerRunning = true;
    }

    private async Task GenerateMappingFile(string tempBuildPath)
    {
        Process? makePackageProcess;
        ArrayList processOutput = [];
        string cmdFormat = "genmap /f {0} /d {1}";

        string layoutFile = Path.Combine(tempBuildPath, "generated_layout.xml");

        makePackageProcess = new Process();
        makePackageProcess.StartInfo.FileName = _pathConfigurationService.MakePkgPath;
        makePackageProcess.StartInfo.Arguments = string.Format(cmdFormat, layoutFile, GameDataPath);
        makePackageProcess.StartInfo.RedirectStandardOutput = true;
        makePackageProcess.StartInfo.RedirectStandardError = true;
        makePackageProcess.EnableRaisingEvents = true;
        makePackageProcess.StartInfo.CreateNoWindow = true;

        makePackageProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        makePackageProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        makePackageProcess.Exited += (sender, args) =>
        {
            makePackageProcess.WaitForExit();

            if (makePackageProcess.ExitCode != 0)
            {
                // Show error message
                ErrorMessage = "Error generating layout file.";
                return;
            }
            MappingDataXmlPath = layoutFile;
        };

        processOutput.Clear();
        makePackageProcess.Start();
        makePackageProcess.BeginOutputReadLine();
        makePackageProcess.BeginErrorReadLine();

        await makePackageProcess.WaitForExitAsync();
    }

    [GeneratedRegex(@"ProductId is '\{(?<ProductId>[a-fA-F0-9]{8}(-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12})\}' derived from '(?<BigId>\w*?)'")]
    private static partial Regex ProductIdRegex();

    [GeneratedRegex(@"Successfully created package '(?<PackagePath>.*?\.xvc)'")]
    private static partial Regex XvcPackagePathRegex();

    [GeneratedRegex(@"Successfully created package '(?<PackagePath>.*?\.msixvc)'")]
    private static partial Regex MsixvcPackagePathRegex();

    [GeneratedRegex(@"See the Submission Validator log file at '(?<SubValFile>.*?\.xml)' for details")]
    private static partial Regex SubValFilePathRegex();

    private void ProcessMakePackageOutput(string outputString, string tempBuildPath)
    {
        MatchCollection productIdMatchCollection = ProductIdRegex().Matches(outputString);
        
        for (int i = 0; i < productIdMatchCollection.Count; i++)
        {
            string bigIdValue = productIdMatchCollection[i].Groups["BigId"].Value;
            
            if (bigIdValue != null)
            {
                BigId = bigIdValue;
                break;
            }
        }
        
        // Package Path
        MatchCollection packagePathMatchCollection = XvcPackagePathRegex().Matches(outputString);
        
        for (int i = 0; i < packagePathMatchCollection.Count; i++)
        {
            string packagePathValue = packagePathMatchCollection[i].Groups["PackagePath"].Value;
            if (packagePathValue != null)
            {
                PackageFilePath = packagePathValue;
                break;
            }
        }

        // Could also be an MSIXVC
        packagePathMatchCollection = MsixvcPackagePathRegex().Matches(outputString);

        for (int i = 0; i < packagePathMatchCollection.Count; i++)
        {
            string packagePathValue = packagePathMatchCollection[i].Groups["PackagePath"].Value;
            if (packagePathValue != null)
            {
                PackageFilePath = packagePathValue;
                break;
            }
        }

        // Ekb File Path
        DirectoryInfo dirInf = new(tempBuildPath);
        foreach (FileInfo file in dirInf.EnumerateFiles())
        {
            if (file.Extension == ".ekb")
            {
                EkbFilePath = file.FullName;
                break;
            }
        }
        
        // SubVal File Path
        MatchCollection subValFilePathMatchCollection = SubValFilePathRegex().Matches(outputString);
        
        for (int i = 0; i < subValFilePathMatchCollection.Count; i++)
        {
            string subValPathValue = subValFilePathMatchCollection[i].Groups["SubValFile"].Value;
            if (subValPathValue != null)
            {
                SubValFilePath = subValPathValue;
                break;
            }
        }
    }
}
