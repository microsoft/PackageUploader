// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace PackageUploader.FileLogger;

internal class FileWriter : IFileWriter
{
    private readonly StreamWriter _streamWriter;

    public FileWriter(FileWriterOptions options)
    {
        var path = options.Path ?? $"Log_{DateTime.Now:yyyyMMddhhmmss}.txt";

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Stream outputStream = File.Open(path, options.Append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        _streamWriter = new StreamWriter(outputStream, options.Encoding ?? new UTF8Encoding(false));
    }

    public void Write(string message)
    {
        _streamWriter.Write(message);
        _streamWriter.Flush();
    }

    public void Dispose()
    {
        _streamWriter?.Dispose();
    }
}