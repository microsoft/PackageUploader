// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace PackageUploader.Application;

internal class Program
{
    internal static string[] RawArgs;

    private static async Task<int> Main(string[] args)
    {
        RawArgs = args;
        var rootCommand = CommandLineHelper.BuildRootCommand();
        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }
}