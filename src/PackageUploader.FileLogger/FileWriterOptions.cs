// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace PackageUploader.FileLogger
{
    public class FileWriterOptions
    {
        public string Path { get; set; }
        public Encoding Encoding { get; set; }
        public bool Append { get; set; }
    }
}