// Copyright (C) Microsoft. All rights reserved.

using System.IO;

namespace GameStoreBroker.Application
{
    internal class Options
    {
        public FileInfo ConfigFile { get; set; }
        public string ClientSecret { get; set; }
    }
}