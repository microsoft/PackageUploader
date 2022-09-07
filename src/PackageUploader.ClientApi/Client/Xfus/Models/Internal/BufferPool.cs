// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal;

internal sealed class BufferPool
{
    private readonly ConcurrentBag<byte[]> _pool;
    private readonly long _bufferSize;

    /// <summary>
    /// Simple unbounded byte array pool
    /// </summary>
    /// <param name="bufferSize">The size of the buffers inside the pool.</param>
    public BufferPool(int bufferSize)
    {
        _bufferSize = bufferSize;
        _pool = new ConcurrentBag<byte[]>();
    }

    /// <summary>
    /// Simple unbounded byte array pool
    /// </summary>
    /// <param name="bufferSize">The size of the buffers inside the pool.</param>
    public BufferPool(long bufferSize)
    {
        _bufferSize = bufferSize;
        _pool = new ConcurrentBag<byte[]>();
    }

    /// <summary>
    /// Gets a buffer from the pool, if there are no more buffers
    /// available it instantiates a new one and returns it to the caller.
    /// </summary>
    /// <returns>A buffer from the pool or a new one if the pool was empty.</returns>
    public byte[] GetBuffer()
    {
        return _pool.TryTake(out var buffer) ? buffer : new byte[_bufferSize];
    }

    /// <summary>
    /// Returns a buffer to the pool, if the buffer does not have
    /// a matching size with other buffers in the pool it gets discarded.
    /// </summary>
    /// <param name="buffer">The buffer to recycle.</param>
    public void RecycleBuffer(byte[] buffer)
    {
        if (buffer != null && buffer.Length == _bufferSize)
        {
            _pool.Add(buffer);
        }
    }
}