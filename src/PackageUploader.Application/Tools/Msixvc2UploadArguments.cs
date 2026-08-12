// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.Application.Tools;

/// <summary>
/// The MakePkg.exe command line in two forms: the one to execute, and the one that is safe to log.
///
/// They are returned together, and built together, so that a caller cannot accidentally log the executable
/// form. <see cref="RedactedCommandLine"/> is not derived from <see cref="CommandLine"/> by scrubbing it —
/// it is built independently from credential-free inputs, so no credential is ever present in the value
/// handed to a logger.
/// </summary>
/// <param name="CommandLine">
/// The real command line, including any credential material. Pass this to the process, never to a log.
/// </param>
/// <param name="RedactedCommandLine">
/// The same command line with credential values replaced by a placeholder. Identical to
/// <paramref name="CommandLine"/> when there was no credential to replace.
/// </param>
internal sealed record Msixvc2UploadArguments(string CommandLine, string RedactedCommandLine);
