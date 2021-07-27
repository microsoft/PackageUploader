// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GameStoreBroker.FileLogger
{
    /// <summary>
    /// Options for the built-in default file log formatter.
    /// </summary>
    public class SimpleFileFormatterOptions : FileFormatterOptions
    {
        /// <summary>
        /// When <see langword="false" />, the entire message gets logged in a single line.
        /// </summary>
        public bool SingleLine { get; set; }
    }
}
