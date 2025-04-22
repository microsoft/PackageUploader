// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace PackageUploader.UI.Utility
{
    internal class DragDropHelper
    {
        public static void RegisterTextBoxDragDrop(System.Windows.Controls.TextBox textBox, Action<string> onDropAction, bool acceptFolders)
        {
            if (textBox == null) return;

            textBox.AllowDrop = true;

            textBox.PreviewDragOver += (sender, e) =>
            {
                e.Effects = System.Windows.DragDropEffects.None;

                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }

                e.Handled = true;
            };

            textBox.Drop += (sender, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        string path = files[0];

                        // Check if the path is valid based on whether we accept folders
                        bool isValid = (acceptFolders && Directory.Exists(path)) ||
                                      (!acceptFolders && File.Exists(path));

                        if (isValid)
                        {
                            onDropAction?.Invoke(path);
                        }
                    }
                }

                e.Handled = true;
            };
        }

        public static void RegisterTextBoxDragDrop(System.Windows.Controls.TextBox textBox, System.Windows.Input.ICommand command, bool acceptFolders)
        {
            if (textBox == null) return;

            textBox.AllowDrop = true;

            textBox.PreviewDragOver += (sender, e) =>
            {
                e.Effects = System.Windows.DragDropEffects.None;

                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                }

                e.Handled = true;
            };

            textBox.Drop += (sender, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        string path = files[0];

                        // Check if the path is valid based on whether we accept folders
                        bool isValid = (acceptFolders && Directory.Exists(path)) ||
                                      (!acceptFolders && File.Exists(path));

                        if (isValid && command != null && command.CanExecute(path))
                        {
                            command.Execute(path);
                        }
                    }
                }

                e.Handled = true;
            };
        }
    }
}
