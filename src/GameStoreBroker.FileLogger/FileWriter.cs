// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace GameStoreBroker.FileLogger
{
    internal class FileWriter : IFileWriter
    {
        private readonly TextWriter _textWriter;

        public FileWriter(FileWriterOptions options)
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
