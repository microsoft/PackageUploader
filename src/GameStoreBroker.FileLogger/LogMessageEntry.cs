// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GameStoreBroker.FileLogger
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
