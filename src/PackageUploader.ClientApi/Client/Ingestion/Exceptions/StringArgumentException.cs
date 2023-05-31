// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PackageUploader.ClientApi.Client.Ingestion.Exceptions;

internal static class StringArgumentException
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null, empty or consists only of white-space characters.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrWhiteSpace([NotNull] string argument, [CallerArgumentExpression("argument")] string paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Value cannot be null, empty or consist only of white-space characters.", paramName);
    }
}