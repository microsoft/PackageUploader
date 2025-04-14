// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System;
using System.Linq;

namespace PackageUploader.UI.Controls;

public partial class DropArea : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(DropArea), 
            new PropertyMetadata("Drag and drop files here or click to browse"));
    
    public static readonly DependencyProperty FileDroppedCommandProperty =
        DependencyProperty.Register(nameof(FileDroppedCommand), typeof(ICommand), typeof(DropArea));
    
    public static readonly DependencyProperty BrowseCommandProperty =
        DependencyProperty.Register(nameof(BrowseCommand), typeof(ICommand), typeof(DropArea));
    
    public static readonly DependencyProperty AcceptedExtensionsProperty =
        DependencyProperty.Register(nameof(AcceptedExtensions), typeof(string), typeof(DropArea), 
            new PropertyMetadata(".*"));
    
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

    private const string FolderExtension = ":folder:";

    public DropArea()
    {
        InitializeComponent();
        
        // Enable drag and drop
        this.AllowDrop = true;
        this.DragEnter += DropArea_DragEnter;
        this.DragOver += DropArea_DragOver;
        this.Drop += DropArea_Drop;
    }

    private void DropArea_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            
            if (files != null && files.Length > 0)
            {
                var acceptedExts = AcceptedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.ToLowerInvariant())
                    .ToArray();
                
                bool acceptsFolders = acceptedExts.Contains(FolderExtension);
                bool acceptsFiles = acceptedExts.Length == 0 || acceptedExts.Contains(".*") || 
                    acceptedExts.Any(ext => ext != FolderExtension);
                
                bool isAccepted = false;
                
                foreach (string filePath in files)
                {
                    if (Directory.Exists(filePath))
                    {
                        if (acceptsFolders)
                        {
                            isAccepted = true;
                            break;
                        }
                    }
                    else if (File.Exists(filePath))
                    {
                        if (acceptsFiles)
                        {
                            if (acceptedExts.Length == 0 || acceptedExts.Contains(".*"))
                            {
                                isAccepted = true;
                                break;
                            }
                            
                            string extension = Path.GetExtension(filePath).ToLowerInvariant();
                            if (acceptedExts.Any(ext => ext == extension))
                            {
                                isAccepted = true;
                                break;
                            }
                        }
                    }
                }
                
                e.Effects = isAccepted ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void DropArea_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void DropArea_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[]? files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            
            if (files == null || files.Length == 0)
                return;
            
            var acceptedExts = AcceptedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.ToLowerInvariant())
                .ToArray();
            
            bool acceptsFolders = acceptedExts.Contains(FolderExtension);
            bool acceptsFiles = acceptedExts.Length == 0 || acceptedExts.Contains(".*") || 
                acceptedExts.Any(ext => ext != FolderExtension);
            
            foreach (string filePath in files)
            {
                if (Directory.Exists(filePath))
                {
                    if (acceptsFolders)
                    {
                        FileDroppedCommand?.Execute(filePath);
                        break;
                    }
                }
                else if (File.Exists(filePath))
                {
                    if (acceptsFiles)
                    {
                        if (acceptedExts.Length == 0 || acceptedExts.Contains(".*"))
                        {
                            FileDroppedCommand?.Execute(filePath);
                            break;
                        }
                        
                        string extension = Path.GetExtension(filePath).ToLowerInvariant();
                        if (acceptedExts.Any(ext => ext == extension))
                        {
                            FileDroppedCommand?.Execute(filePath);
                            break;
                        }
                    }
                }
            }
        }
        
        e.Handled = true;
    }
}
