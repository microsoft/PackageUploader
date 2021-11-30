// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace PackageUploader.FileLogger
{
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.AddFileFormatter<JsonFileFormatter, JsonFileFormatterOptions>();
            builder.AddFileFormatter<SimpleFileFormatter, SimpleFileFormatterOptions>();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);
            LoggerProviderOptions.RegisterProviderOptions<FileWriterOptions, FileLoggerProvider>(builder.Services);

            return builder;
        }

        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/>.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configure) =>
            AddFile(builder, configure, _ => { });

        /// <summary>
        /// Adds a file logger named 'File' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/>.</param>
        /// <param name="configureFile">A delegate to configure the <see cref="FileWriter"/>.</param>
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileLoggerOptions> configure, Action<FileWriterOptions> configureFile)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (configureFile == null)
            {
                throw new ArgumentNullException(nameof(configureFile));
            }

            builder.AddFile();
            builder.Services.Configure(configure);
            builder.Services.Configure(configureFile);

            return builder;
        }

        /// <summary>
        /// Add the default file log formatter named 'simple' to the factory with default properties.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder) =>
            builder.AddFormatterWithName(FileFormatterNames.Simple);

        /// <summary>
        /// Add and configure a file log formatter named 'simple' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in default log formatter.</param>
        public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, Action<SimpleFileFormatterOptions> configure)
        {
            return builder.AddFileWithFormatter(FileFormatterNames.Simple, configure);
        }

        /// <summary>
        /// Add and configure a file log formatter named 'simple' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in default log formatter.</param>
        /// <param name="configureFile">A delegate to configure the <see cref="FileWriter"/> options.</param>
        public static ILoggingBuilder AddSimpleFile(this ILoggingBuilder builder, Action<SimpleFileFormatterOptions> configure, Action<FileWriterOptions> configureFile)
        {
            return builder.AddFileWithFormatter(FileFormatterNames.Simple, configure, configureFile);
        }

        /// <summary>
        /// Add a file log formatter named 'json' to the factory with default properties.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder) =>
            builder.AddFormatterWithName(FileFormatterNames.Json);

        /// <summary>
        /// Add and configure a file log formatter named 'json' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in json log formatter.</param>
        public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder, Action<JsonFileFormatterOptions> configure)
        {
            return builder.AddFileWithFormatter(FileFormatterNames.Json, configure);
        }

        /// <summary>
        /// Add and configure a file log formatter named 'json' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="FileLogger"/> options for the built-in json log formatter.</param>
        /// <param name="configureFile">A delegate to configure the <see cref="FileWriter"/> options.</param>
        public static ILoggingBuilder AddJsonFile(this ILoggingBuilder builder, Action<JsonFileFormatterOptions> configure, Action<FileWriterOptions> configureFile)
        {
            return builder.AddFileWithFormatter(FileFormatterNames.Json, configure, configureFile);
        }

        internal static ILoggingBuilder AddFileWithFormatter<TOptions>(this ILoggingBuilder builder, string name,
            Action<TOptions> configure) where TOptions : FileFormatterOptions =>
            AddFileWithFormatter(builder, name, configure, _ => { });

        internal static ILoggingBuilder AddFileWithFormatter<TOptions>(this ILoggingBuilder builder, string name, Action<TOptions> configure, Action<FileWriterOptions> configureFile)
            where TOptions : FileFormatterOptions
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            builder.AddFormatterWithName(name);
            builder.Services.Configure(configure);
            builder.Services.Configure(configureFile);

            return builder;
        }

        private static ILoggingBuilder AddFormatterWithName(this ILoggingBuilder builder, string name)
        {
            return builder.AddFile(options => options.FormatterName = name);
        }

        /// <summary>
        /// Adds a custom file logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder)
            where TOptions : FileFormatterOptions
            where TFormatter : FileFormatter
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<FileFormatter, TFormatter>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, FileLoggerFormatterConfigureOptions<TOptions>>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, FileLoggerFormatterOptionsChangeTokenSource<TOptions>>());

            return builder;
        }

        /// <summary>
        /// Adds a custom file logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure options 'TOptions' for custom formatter 'TFormatter'.</param>
        public static ILoggingBuilder AddFileFormatter<TFormatter, TOptions>(this ILoggingBuilder builder, Action<TOptions> configure)
            where TOptions : FileFormatterOptions
            where TFormatter : FileFormatter
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddFileFormatter<TFormatter, TOptions>();
            builder.Services.Configure(configure);
            return builder;
        }
    }

    internal class FileLoggerFormatterConfigureOptions<TOptions> : ConfigureFromConfigurationOptions<TOptions>
        where TOptions : FileFormatterOptions
    {
        public FileLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration) :
            base(providerConfiguration.Configuration.GetSection("FormatterOptions"))
        {
        }
    }

    internal class FileLoggerFormatterOptionsChangeTokenSource<TOptions> : ConfigurationChangeTokenSource<TOptions>
        where TOptions : FileFormatterOptions
    {
        public FileLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration.GetSection("FormatterOptions"))
        {
        }
    }
}
