// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;

namespace GameStoreBroker.FileLogger
{
    internal class LogFile : IFile
    {
        private readonly TextWriter _textWriter;

        public LogFile(FileWriterOptions options)
        {
            var path = options.Path ?? $"Log_{DateTime.Now:yyyyMMddhhmmss}.txt";

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Stream outputStream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            _textWriter = new StreamWriter(outputStream, options.Encoding ?? new UTF8Encoding(false));
        }

        public void Write(string message)
        {
            _textWriter.Write(message);
            _textWriter.Flush();
        }

        public void Dispose()
        {
            _textWriter?.Dispose();
        }
    }
}
