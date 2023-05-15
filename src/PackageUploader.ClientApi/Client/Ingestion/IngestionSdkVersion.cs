// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace PackageUploader.ClientApi.Client.Ingestion;

internal class IngestionSdkVersion
{
    public string SdkVersion { get; }

    public IngestionSdkVersion()
    {
        SdkVersion = GetSdkVersion();
    }

    private static string GetSdkVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        return assemblyVersionAttribute is null ?
            $"SDK-V{assembly.GetName().Version?.ToString() ?? "1.0.0"}" :
            $"SDK-V{assemblyVersionAttribute.InformationalVersion}";
    }
}