// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.FileLogger
{
    internal readonly struct LogMessageEntry
    {
        public LogMessageEntry(string message)
        {
            Message = message;
        }

        public readonly string Message;
    }
}
