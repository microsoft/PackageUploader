// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
