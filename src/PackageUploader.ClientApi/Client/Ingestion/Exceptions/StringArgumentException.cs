// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PackageUploader.ClientApi.Client.Ingestion.Exceptions;

internal static class StringArgumentException
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null, empty or consists only of white-space characters.</summary>
    /// <param name="argument">The reference type argument to validate.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrWhiteSpace([NotNull] string argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            ArgumentNullException.ThrowIfNull(argument);
            throw new ArgumentException("Argument is empty or consists only of white-space characters", paramName);
        }
    }
}