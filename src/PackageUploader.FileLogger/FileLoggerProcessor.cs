// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PackageUploader.FileLogger
{
    internal class FileLoggerProcessor : IDisposable
    {
        private const int MaxQueuedMessages = 1024;

        private readonly BlockingCollection<LogMessageEntry> _messageQueue = new BlockingCollection<LogMessageEntry>(MaxQueuedMessages);
        private readonly Thread _outputThread;

        public IFileWriter FileWriter;

        public FileLoggerProcessor()
        {
            // Start File message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "File logger queue processing thread"
            };
            _outputThread.Start();
        }

        public virtual void EnqueueMessage(LogMessageEntry message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }

            // Adding is completed so just log the message
            try
            {
                WriteMessage(message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        // for testing
        internal virtual void WriteMessage(LogMessageEntry entry)
        {
            FileWriter.Write(entry.Message);
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (LogMessageEntry message in _messageQueue.GetConsumingEnumerable())
                {
                    WriteMessage(message);
                }
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch
                {
                    // ignored
                }
            }
        }

        public void Dispose()
        {
            _messageQueue.CompleteAdding();

            try
            {
                _outputThread.Join(1500); // with timeout in-case it is locked
                FileWriter?.Dispose();
            }
            catch (ThreadStateException) { }
        }
    }
}
