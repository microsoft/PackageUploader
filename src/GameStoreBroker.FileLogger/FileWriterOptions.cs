// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace GameStoreBroker.FileLogger
{
    public class FileWriterOptions
    {
        public string Path { get; set; }
        public Encoding Encoding { get; set; }
    }
}