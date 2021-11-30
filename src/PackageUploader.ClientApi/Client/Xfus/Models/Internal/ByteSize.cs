// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Xfus.Models.Internal
{
    internal class ByteSize
    {
        private const ulong KiloMultiplier = 1024uL;
        private const ulong MegaMultiplier = 1024uL * 1024uL;
        private const ulong GigaMultiplier = 1024uL * 1024uL * 1024uL;
        private const ulong TeraMultiplier = 1024uL * 1024uL * 1024uL * 1024uL;
        private const ulong PetaMultiplier = 1024uL * 1024uL * 1024uL * 1024uL * 1024uL;
        private const ulong ExaMultiplier = 1024uL * 1024uL * 1024uL * 1024uL * 1024uL * 1024uL;
        private static readonly Dictionary<char, ulong> SizeMultipliers = new Dictionary<char, ulong>
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
            this.SizeInBytes = bytes;
        }

        public ByteSize(long bytes)
        {
            this.SizeInBytes = (ulong)bytes;
        }

        public override string ToString()
        {
            foreach (var prefixToSize in SizeMultipliers)
            {
                if (this.SizeInBytes >= prefixToSize.Value)
                {
                    var size = (double)this.SizeInBytes / (double)prefixToSize.Value;

                    return size.ToString("0.##") + prefixToSize.Key.ToString() + "B";
                }
            }

            return this.SizeInBytes + "B";
        }
    }
}
