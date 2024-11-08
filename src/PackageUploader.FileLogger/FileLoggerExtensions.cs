// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace PackageUploader.FileLogger;

public static class FileLoggerExtensions
{
    internal const string RequiresDynamicCodeMessage = "Binding TOptions to configuration values may require generating dynamic code at runtime.";
    internal const string TrimmingRequiresUnreferencedCodeMessage = "TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.";

    /// <summary>
    /// Adds a file logger named 'File' to the factory.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.AddFileFormatter<JsonFileFormatter, JsonFileFormatterOptions, FileFormatterConfigureOptions>();
        builder.AddFileFormatter<SimpleFileFormatter, SimpleFileFormatterOptions, FileFormatterConfigureOptions>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FileLoggerOptions>, FileLoggerConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<FileLoggerOptions>, LoggerProviderOptionsChangeTokenSource<FileLoggerOptions, FileLoggerProvider>>());

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
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(configureFile);

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
        ArgumentNullException.ThrowIfNull(configure);

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
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
    public static ILoggingBuilder AddFileFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this ILoggingBuilder builder)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
    {
        return AddFileFormatter<TFormatter, TOptions, FileLoggerFormatterConfigureOptions<TFormatter, TOptions>>(builder);
    }

    /// <summary>
    /// Adds a custom file logger formatter 'TFormatter' to be configured with options 'TOptions'.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="configure">A delegate to configure options 'TOptions' for custom formatter 'TFormatter'.</param>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
    public static ILoggingBuilder AddFileFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this ILoggingBuilder builder, Action<TOptions> configure)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddFileFormatter<TFormatter, TOptions>();
        builder.Services.Configure(configure);
        return builder;
    }

    private static ILoggingBuilder AddFileFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConfigureOptions>(this ILoggingBuilder builder)
        where TOptions : FileFormatterOptions
        where TFormatter : FileFormatter
        where TConfigureOptions : class, IConfigureOptions<TOptions>
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<FileFormatter, TFormatter>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, TConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, FileLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions>>());

        return builder;
    }

    internal static IConfiguration GetFormatterOptionsSection(this ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        return providerConfiguration.Configuration.GetSection("FormatterOptions");
    }
}

internal sealed class FileLoggerFormatterConfigureOptions<TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions> : ConfigureFromConfigurationOptions<TOptions>
    where TOptions : FileFormatterOptions
    where TFormatter : FileFormatter
{
    [RequiresDynamicCode(FileLoggerExtensions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FileLoggerExtensions.TrimmingRequiresUnreferencedCodeMessage)]
    public FileLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration) :
        base(providerConfiguration.GetFormatterOptionsSection())
    {
    }
}

internal sealed class FileLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions> : ConfigurationChangeTokenSource<TOptions>
    where TOptions : FileFormatterOptions
    where TFormatter : FileFormatter
{
    public FileLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
        : base(providerConfiguration.GetFormatterOptionsSection())
    {
    }
}