// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.IntegrationTest.Fixtures;

/// <summary>Creates tiny, self-deleting synthetic package files for upload-path tests (not valid game content).</summary>
internal sealed class SyntheticPackageFile : IDisposable
{
    public string Path { get; }

    public long SizeInBytes { get; }

    private SyntheticPackageFile(string path, long sizeInBytes)
    {
        Path = path;
        SizeInBytes = sizeInBytes;
    }

    public static SyntheticPackageFile Create(long sizeInBytes = 64 * 1024, string extension = ".xvc", int seed = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeInBytes);

        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"pkguploader-it-{Guid.NewGuid():N}{extension}");

        var random = new Random(seed);
        const int bufferSize = 8 * 1024;
        var buffer = new byte[bufferSize];

        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            long remaining = sizeInBytes;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min(bufferSize, remaining);
                random.NextBytes(buffer.AsSpan(0, chunk));
                stream.Write(buffer, 0, chunk);
                remaining -= chunk;
            }
        }

        return new SyntheticPackageFile(path, sizeInBytes);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch (Exception)
        {
            // Best-effort cleanup; a leaked temp file must never fail a test.
        }
    }
}
