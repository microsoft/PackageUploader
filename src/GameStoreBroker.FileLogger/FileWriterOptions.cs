// Copyright (C) Microsoft. All rights reserved.

using System.Text;

namespace GameStoreBroker.FileLogger
{
    public class FileWriterOptions
    {
        public string Path { get; set; }
        public Encoding Encoding { get; set; }
    }
}