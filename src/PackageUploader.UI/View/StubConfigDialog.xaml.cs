// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using PackageUploader.UI.Utility;
using Strings = PackageUploader.UI.Resources.Strings.PackageCreation;

namespace PackageUploader.UI.View;

public partial class StubConfigDialog : Window
{
    public string SelectedPlatform { get; private set; } = string.Empty;
    public string SelectedGdkVersion { get; private set; } = string.Empty;
    public string MsBuildPath { get; private set; } = string.Empty;

    public StubConfigDialog()
    {
        InitializeComponent();

        // Set localized text
        Title = Strings.StubConfigDialogTitle;
        PlatformLabel.Text = Strings.StubConfigPlatformLabel;
        GdkVersionLabel.Text = Strings.StubConfigGdkVersionLabel;
        BuildButton.Content = Strings.StubConfigBuildButtonText;

        PlatformCombo.SelectedIndex = 0;
        DetectEnvironment();
    }

    private void DetectEnvironment()
    {
        // Enumerate GDK versions
        var versions = StubBuilder.EnumerateGdkVersions();
        GdkVersionCombo.Items.Clear();

        if (versions.Count == 0)
        {
            MsBuildStatusText.Text = Strings.StubConfigNoGdkMsg;
            BuildButton.IsEnabled = false;
            return;
        }

        for (int i = 0; i < versions.Count; i++)
        {
            string display = i == 0 ? $"{versions[i]} (latest)" : versions[i];
            GdkVersionCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = display,
                Tag = versions[i]
            });
        }
        GdkVersionCombo.SelectedIndex = 0;

        // Detect MSBuild
        MsBuildPath = StubBuilder.FindMsBuild();
        if (string.IsNullOrEmpty(MsBuildPath))
        {
            MsBuildStatusText.Text = Strings.StubConfigMsBuildNotFoundMsg;
            BuildButton.IsEnabled = false;
        }
        else
        {
            MsBuildStatusText.Text = string.Format(
                Strings.StubConfigMsBuildFoundMsg, MsBuildPath);
            BuildButton.IsEnabled = true;
        }
    }

    private void PlatformCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Validate Xbox build requirements when platform changes
        if (PlatformCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
        {
            string platform = item.Tag?.ToString() ?? "PC";
            if (platform != "PC" && !string.IsNullOrEmpty(MsBuildPath))
            {
                // For Xbox builds, verify GDK has GXDK directory
                var selectedGdkItem = GdkVersionCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
                string gdkVersion = selectedGdkItem?.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(gdkVersion))
                {
                    string gdkRoot = StubBuilder.GetGdkRootPath();
                    string gxdkPath = System.IO.Path.Combine(gdkRoot, gdkVersion, "GXDK");
                    if (!System.IO.Directory.Exists(gxdkPath))
                    {
                        MsBuildStatusText.Text = string.Format(
                            Strings.StubConfigMsBuildFoundMsg, MsBuildPath)
                            + "\n⚠ Selected GDK version does not include Xbox extensions (GXDK).";
                    }
                }
            }
        }
    }

    private void BuildButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlatformCombo.SelectedItem is System.Windows.Controls.ComboBoxItem platformItem)
            SelectedPlatform = platformItem.Tag?.ToString() ?? "PC";

        if (GdkVersionCombo.SelectedItem is System.Windows.Controls.ComboBoxItem gdkItem)
            SelectedGdkVersion = gdkItem.Tag?.ToString() ?? "";

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
