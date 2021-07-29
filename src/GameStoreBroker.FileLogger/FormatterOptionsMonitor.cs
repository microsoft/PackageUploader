// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;

namespace GameStoreBroker.FileLogger
{
    internal class FormatterOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : FileFormatterOptions
    {
        private readonly TOptions _options;
        public FormatterOptionsMonitor(TOptions options)
        {
            _options = options;
        }

        public TOptions Get(string name) => _options;

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            return null;
        }

        public TOptions CurrentValue => _options;
    }
}
