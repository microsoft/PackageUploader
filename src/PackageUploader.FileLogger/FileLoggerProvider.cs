// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PackageUploader.FileLogger;

/// <summary>
/// A provider of <see cref="FileLogger"/> instances.
/// </summary>
[ProviderAlias("File")]
public class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IOptionsMonitor<FileLoggerOptions> _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers;
    private ConcurrentDictionary<string, FileFormatter> _formatters;
    private readonly FileLoggerProcessor _messageQueue;

    private readonly IDisposable _optionsReloadToken;
    private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

    /// <summary>
    /// Creates an instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to create <see cref="FileLogger"/> instances with.</param>
    /// <param name="fileWriterOptions">The options to create <see cref="FileWriter"/> instance with.</param>
    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IOptionsMonitor<FileWriterOptions> fileWriterOptions)
        : this(options, fileWriterOptions, Enumerable.Empty<FileFormatter>()) { }

    /// <summary>
    /// Creates an instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options to create <see cref="FileLogger"/> instances with.</param>
    /// <param name="fileWriterOptions">The options to create <see cref="FileWriter"/> instance with.</param>
    /// <param name="formatters">Log formatters added for <see cref="FileLogger"/> instances.</param>
    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options, IOptionsMonitor<FileWriterOptions> fileWriterOptions, IEnumerable<FileFormatter> formatters)
    {
        _options = options;
        _loggers = new ConcurrentDictionary<string, FileLogger>();
        SetFormatters(formatters);

        ReloadLoggerOptions(options.CurrentValue);
        _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
            
        _messageQueue = new FileLoggerProcessor
        {
            FileWriter = new FileWriter(fileWriterOptions.CurrentValue)
        };
    }

    private void SetFormatters(IEnumerable<FileFormatter> formatters = null)
    {
        _formatters = new ConcurrentDictionary<string, FileFormatter>(StringComparer.OrdinalIgnoreCase);
        if (formatters == null || !formatters.Any())
        {
            var defaultMonitor = new FormatterOptionsMonitor<SimpleFileFormatterOptions>(new SimpleFileFormatterOptions());
            var jsonMonitor = new FormatterOptionsMonitor<JsonFileFormatterOptions>(new JsonFileFormatterOptions());
            _formatters.GetOrAdd(FileFormatterNames.Simple, formatterName => new SimpleFileFormatter(defaultMonitor));
            _formatters.GetOrAdd(FileFormatterNames.Json, formatterName => new JsonFileFormatter(jsonMonitor));
        }
        else
        {
            foreach (FileFormatter formatter in formatters)
            {
                _formatters.GetOrAdd(formatter.Name, formatterName => formatter);
            }
        }
    }

    // warning:  ReloadLoggerOptions can be called before the ctor completed,... before registering all of the state used in this method need to be initialized
    private void ReloadLoggerOptions(FileLoggerOptions options)
    {
        if (options.FormatterName != null && _formatters.TryGetValue(options.FormatterName, out FileFormatter logFormatter))
        {
            foreach (KeyValuePair<string, FileLogger> logger in _loggers)
            {
                logger.Value.Options = options;
                logger.Value.Formatter = logFormatter;
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string name)
    {
        if (_options.CurrentValue.FormatterName != null && _formatters.TryGetValue(_options.CurrentValue.FormatterName, out FileFormatter logFormatter))
        {
            return _loggers.GetOrAdd(name, loggerName => new FileLogger(name, _messageQueue)
            {
                Options = _options.CurrentValue,
                ScopeProvider = _scopeProvider,
                Formatter = logFormatter,
            });
        }
        throw new Exception("FileFormatter not found");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
        _messageQueue.Dispose();
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;

        foreach (KeyValuePair<string, FileLogger> logger in _loggers)
        {
            logger.Value.ScopeProvider = _scopeProvider;
        }
    }
}