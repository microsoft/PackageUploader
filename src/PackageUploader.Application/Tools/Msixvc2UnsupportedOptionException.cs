// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Thrown when the operation configuration contains an option that MakePkg.exe has no equivalent for.
/// The CLI fails fast rather than silently dropping the option.
/// </summary>
internal sealed class Msixvc2UnsupportedOptionException(string message) : Exception(message);
