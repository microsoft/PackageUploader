// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi.Tools;
using System;

namespace PackageUploader.Application.Tools;

/// <summary>
/// Adapts <see cref="IMsixvc2ToolResolver"/> from PackageUploader.ClientApi to this project's
/// <see cref="IMsixvc2UploadToolProvider"/>.
///
/// Resolution is performed with no path hints, i.e. pure self-discovery (application directory,
/// current directory, the installed GDK, then PATH). The UI passes already-resolved paths because it
/// has its own file pickers; the CLI has no such input and deliberately relies on self-discovery.
/// </summary>
/// <remarks>
/// <para>
/// The underlying resolver intentionally does not cache: every <c>Resolve()</c> call re-probes a
/// candidate executable by launching it. This adapter therefore resolves EXACTLY ONCE and serves both
/// members from that single result, which satisfies the "single underlying resolution" clause of
/// <see cref="IMsixvc2UploadToolProvider"/>'s contract. Reading both members must not launch the probe
/// twice, and must not be able to report an available tool alongside a null path if the environment
/// changes mid-operation.
/// </para>
/// <para>
/// This type is registered per-scope so that each operation gets a fresh resolution rather than one
/// cached for the lifetime of the process.
/// </para>
/// </remarks>
internal sealed class Msixvc2ToolResolverAdapter : IMsixvc2UploadToolProvider
{
    private readonly IMsixvc2ToolResolver _resolver;
    private readonly ILogger<Msixvc2ToolResolverAdapter> _logger;

    private bool _resolved;
    private Msixvc2Tool _tool;

    public Msixvc2ToolResolverAdapter(IMsixvc2ToolResolver resolver, ILogger<Msixvc2ToolResolverAdapter> logger)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _logger = logger;
    }

    /// <summary>
    /// True when <see cref="IMsixvc2ToolResolver.Resolve()"/> produced a tool. A null result means
    /// "no MSIXVC2-capable tool is installed", which is an ordinary outcome, not an error.
    /// </summary>
    public bool IsAvailable => ResolveOnce() is not null;

    /// <summary>
    /// The resolved executable path, or <c>null</c> when no capable tool was found. Always consistent
    /// with <see cref="IsAvailable"/> because both read the same cached resolution.
    /// </summary>
    public string ExecutablePath => ResolveOnce()?.ExecutablePath;

    private Msixvc2Tool ResolveOnce()
    {
        if (_resolved)
        {
            return _tool;
        }

        // The resolver is documented as never throwing, returning null for "nothing capable found".
        // This catch is defense in depth only: UploadXvcPackageOperation is written to report an
        // unavailable tool as a clean, actionable message, so an exception escaping here would be a
        // new failure mode it cannot handle. Degrade to "unavailable" instead.
        try
        {
            _tool = _resolver.Resolve();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "MSIXVC2 tool resolution threw; treating MSIXVC2 as unavailable.");
            _tool = null;
        }

        _resolved = true;
        return _tool;
    }
}
