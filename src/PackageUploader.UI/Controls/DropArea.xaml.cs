// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Input;

namespace PackageUploader.UI.Controls;

public partial class DropArea : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(DropArea), "Drag and drop files here or click to browse");
    
    public static readonly BindableProperty FileDroppedCommandProperty =
        BindableProperty.Create(nameof(FileDroppedCommand), typeof(ICommand), typeof(DropArea));
    
    public static readonly BindableProperty BrowseCommandProperty =
        BindableProperty.Create(nameof(BrowseCommand), typeof(ICommand), typeof(DropArea));
    
    public static readonly BindableProperty AcceptedExtensionsProperty =
        BindableProperty.Create(nameof(AcceptedExtensions), typeof(string), typeof(DropArea), ".*");
    
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
    
    public ICommand FileDroppedCommand
    {
        get => (ICommand)GetValue(FileDroppedCommandProperty);
        set => SetValue(FileDroppedCommandProperty, value);
    }
    
    public ICommand BrowseCommand
    {
        get => (ICommand)GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }
    
    public string AcceptedExtensions
    {
        get => (string)GetValue(AcceptedExtensionsProperty);
        set => SetValue(AcceptedExtensionsProperty, value);
    }

    public DropArea()
    {
        InitializeComponent();

        // Set up platform-specific implementation for file dropping
#if WINDOWS
        this.Loaded += OnLoaded!;
#endif
    }

#if WINDOWS

    private const string FolderExtension = ":folder:";

    private void OnLoaded(object? sender, EventArgs e)
    {
        var nativeView = this.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (nativeView == null) return;

        // Set up drag-drop handlers
        nativeView.AllowDrop = true;

        // Handle DragOver - this determines if the drop is allowed
        nativeView.DragOver += (s, args) =>
        {
            bool acceptsFolders = false;
            bool acceptsFiles = false;
            bool isAccepted = false;

            // Check if any of the files have an accepted extension
            if (args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var acceptedExts = AcceptedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.ToLowerInvariant())
                    .ToArray();

                if (acceptedExts.Contains(FolderExtension))
                {
                    acceptsFolders = true;
                }
                
                if (acceptedExts.Length > 1 || !acceptedExts.Contains(FolderExtension))
                {
                    acceptsFiles = true;
                }

                // Accept all if extensions list is empty or contains ".*"
                if (acceptedExts.Length == 0 || acceptedExts.Contains(".*"))
                {
                    isAccepted = true;
                }
                else
                {
                    // Get the deferral to access the files asynchronously
                    var deferral = args.GetDeferral();
                    try
                    {
                        var items = args.DataView.GetStorageItemsAsync().AsTask().GetAwaiter().GetResult();

                        // Check if any file has an accepted extension
                        foreach (var item in items)
                        {
                            if (item is Windows.Storage.StorageFile file)
                            {
                                string extension = Path.GetExtension(file.Path).ToLowerInvariant();
                                if (acceptedExts.Any(ext => extension == ext))
                                {
                                    isAccepted = true;
                                    break;
                                }
                            }
                            else if (item is Windows.Storage.StorageFolder)
                            {
                                if (acceptsFolders)
                                {
                                    isAccepted = true;
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                }

                if (isAccepted)
                {
                    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                }
            }

            if (acceptsFiles)
            {
                args.DragUIOverride.Caption = "Drop to select this file";
            }
            else if (acceptsFolders)
            {
                args.DragUIOverride.Caption = "Drop to select this folder";
            }

            if (!isAccepted)
            {
                args.DragUIOverride.Caption = "This file type is not supported";
            }

            args.DragUIOverride.IsCaptionVisible = true;
            args.DragUIOverride.IsContentVisible = true;

            args.Handled = true;
        };

        // Handle Drop - this processes the dropped files
        nativeView.Drop += async (s, args) =>
        {
            if (args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await args.DataView.GetStorageItemsAsync();

                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        string extension = Path.GetExtension(file.Path).ToLowerInvariant();
                        string[] acceptedExtensions = [.. AcceptedExtensions
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.ToLowerInvariant())];

                        if (acceptedExtensions.Length == 0 || acceptedExtensions[0] == ".*" ||
                            acceptedExtensions.Any(ext => extension == ext || ext == ".*"))
                        {
                            // Execute the command with the file path
                            FileDroppedCommand?.Execute(file.Path);
                            break; // Only take the first valid file
                        }
                    }
                    else if (item is Windows.Storage.StorageFolder folder)
                    {
                        string[] acceptedExtensions = [.. AcceptedExtensions
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.ToLowerInvariant())];
                        if (acceptedExtensions.Contains(FolderExtension))
                        {
                            // Execute the command with the folder path
                            FileDroppedCommand?.Execute(folder.Path);
                            break; // Only take the first valid folder
                        }
                    }
                }

                args.Handled = true;
            }
        };
    }
#endif
}
