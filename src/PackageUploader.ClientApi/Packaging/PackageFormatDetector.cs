// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace PackageUploader.ClientApi.Packaging;

/// <summary>
/// Detects the on-disk format of game packages. Lives in PackageUploader.ClientApi so that both
/// PackageUploader.Application (CLI) and PackageUploader.UI share a single source of truth.
/// </summary>
public static class PackageFormatDetector
{
    private static readonly byte[] ZipLocalFileSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] ZipEocdSignature = [0x50, 0x4B, 0x05, 0x06];
    private const int FirstReadSize = 4096;
    private const int LastReadSize = 65 * 1024;
    private const int MinFileNameOffset = 30;

    /// <summary>
    /// Detects whether a .msixvc file is in MSIXVC2 format by checking for ZIP signatures.
    /// MSIXVC2 packages are ZIP-based and contain standard ZIP headers, while MSIXVC1
    /// packages use a proprietary binary format without ZIP signatures.
    /// </summary>
    public static bool IsLikelyMsixvc2Package(string packagePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                return false;

            if (!packagePath.EndsWith(".msixvc", StringComparison.OrdinalIgnoreCase))
                return false;

            var fileInfo = new FileInfo(packagePath);
            if (!fileInfo.Exists || fileInfo.Length < FirstReadSize)
                return false;

            using var stream = File.OpenRead(packagePath);

            // Check first bytes for ZIP local file header at offset 0
            var firstBuffer = new byte[Math.Min(FirstReadSize, fileInfo.Length)];
            stream.ReadExactly(firstBuffer, 0, firstBuffer.Length);

            if (firstBuffer.Length >= MinFileNameOffset &&
                firstBuffer[0] == ZipLocalFileSignature[0] &&
                firstBuffer[1] == ZipLocalFileSignature[1] &&
                firstBuffer[2] == ZipLocalFileSignature[2] &&
                firstBuffer[3] == ZipLocalFileSignature[3])
            {
                return true;
            }

            // Check last 65KB for ZIP End of Central Directory signature
            long lastChunkStart = Math.Max(0, fileInfo.Length - LastReadSize);
            int lastChunkSize = (int)(fileInfo.Length - lastChunkStart);
            var lastBuffer = new byte[lastChunkSize];
            stream.Position = lastChunkStart;
            stream.ReadExactly(lastBuffer, 0, lastChunkSize);

            if (LastIndexOfSignature(lastBuffer, ZipEocdSignature) >= 0)
                return true;

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static int LastIndexOfSignature(byte[] buffer, byte[] signature)
    {
        if (buffer.Length < signature.Length)
            return -1;

        for (int i = buffer.Length - signature.Length; i >= 0; i--)
        {
            bool match = true;
            for (int j = 0; j < signature.Length; j++)
            {
                if (buffer[i + j] != signature[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }

        return -1;
    }
}
