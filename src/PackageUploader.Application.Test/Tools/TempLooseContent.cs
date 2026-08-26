// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Test.Tools;

/// <summary>
/// Creates a throwaway loose game content directory: a folder holding a MicrosoftGame.config, which is the
/// shape MakePkg.exe packs from and PackageUploader must refuse.
/// </summary>
internal sealed class TempLooseContent : IDisposable
{
    public string Path { get; }

    public string GameConfigPath => System.IO.Path.Combine(Path, "MicrosoftGame.config");

    private TempLooseContent(string path) => Path = path;

    public static TempLooseContent Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pu-test-loose-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        File.WriteAllText(
            System.IO.Path.Combine(path, "MicrosoftGame.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Game configVersion=\"1\" />");

        return new TempLooseContent(path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }
}
