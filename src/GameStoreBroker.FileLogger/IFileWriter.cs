// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.FileLogger
{
    internal interface IFileWriter : IDisposable
    {
        void Write(string message);
    }
}
