// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace GameStoreBroker.Application
{
    internal class Options
    {
        public FileInfo ConfigFile { get; set; }
        public string ClientSecret { get; set; }
    }
}