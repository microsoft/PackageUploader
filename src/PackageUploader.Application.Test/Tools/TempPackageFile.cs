// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Creates throwaway package files on disk for MSIXVC2 detection tests.
/// </summary>
internal sealed class TempPackageFile : IDisposable
{
    private const int MinimumDetectableSize = 4096;

    public string Path { get; }

    private TempPackageFile(string path) => Path = path;

    /// <summary>Creates a .msixvc file that starts with the ZIP local file header, i.e. an MSIXVC2 package.</summary>
    public static TempPackageFile CreateMsixvc2() => Create(".msixvc", [0x50, 0x4B, 0x03, 0x04]);

    /// <summary>Creates a .msixvc file with no ZIP signatures, i.e. a legacy MSIXVC1/XVC1 package.</summary>
    public static TempPackageFile CreateLegacyXvc() => Create(".msixvc", Encoding.ASCII.GetBytes("msft"));

    private static TempPackageFile Create(string extension, byte[] header)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pu-test-{Guid.NewGuid():N}{extension}");

        var contents = new byte[MinimumDetectableSize * 2];
        Array.Copy(header, contents, header.Length);
        File.WriteAllBytes(path, contents);

        return new TempPackageFile(path);
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
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }
}
