// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal;

internal class ByteSize
{
    private const ulong KiloMultiplier = 1024uL;
    private const ulong MegaMultiplier = 1024uL * 1024uL;
    private const ulong GigaMultiplier = 1024uL * 1024uL * 1024uL;
    private const ulong TeraMultiplier = 1024uL * 1024uL * 1024uL * 1024uL;
    private const ulong PetaMultiplier = 1024uL * 1024uL * 1024uL * 1024uL * 1024uL;
    private const ulong ExaMultiplier = 1024uL * 1024uL * 1024uL * 1024uL * 1024uL * 1024uL;
    private static readonly Dictionary<char, ulong> SizeMultipliers = new()
    {
        { 'E', ExaMultiplier },
        { 'P', PetaMultiplier },
        { 'T', TeraMultiplier },
        { 'G', GigaMultiplier },
        { 'M', MegaMultiplier },
        { 'K', KiloMultiplier },
    };

    public ulong SizeInBytes { get; private set; }

    public ByteSize(ulong bytes)
    {
        SizeInBytes = bytes;
    }

    public ByteSize(long bytes)
    {
        SizeInBytes = (ulong)bytes;
    }

    public override string ToString()
    {
        foreach (var prefixToSize in SizeMultipliers)
        {
            if (SizeInBytes >= prefixToSize.Value)
            {
                var size = (double)SizeInBytes / prefixToSize.Value;

                return size.ToString("0.##") + prefixToSize.Key + "B";
            }
        }

        return SizeInBytes + "B";
    }
}