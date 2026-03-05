// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Windows;

namespace PackageUploader.UI.View;

public partial class BuildOutputDialog : Window
{
    private bool _buildComplete;

    public BuildOutputDialog()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_buildComplete)
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    public void AppendOutput(string text)
    {
        Dispatcher.Invoke(() =>
        {
            OutputTextBox.AppendText(text + "\n");
            OutputTextBox.ScrollToEnd();
        });
    }

    public void SetStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = status;
        });
    }

    public void BuildComplete()
    {
        Dispatcher.Invoke(() =>
        {
            _buildComplete = true;
            CloseButton.IsEnabled = true;
        });
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
