// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application.Operations;

internal abstract class Operation(ILogger logger)
{
    private readonly ILogger _logger = logger;

    public async Task<int> RunAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("PackageUploader is running.");
            await ProcessAsync(ct).ConfigureAwait(false);
            return 0;
        }
        catch (Exception e)
        {
            _logger.LogTrace(e, "Exception thrown.");
            if (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Operation cancelled.");
                return 1;
            }
            _logger.LogError("{errorMessage}", e.Message);
            return 3;
        }
        finally
        {
            _logger.LogInformation("PackageUploader has finished running.");
        }
    }

    protected abstract Task ProcessAsync(CancellationToken ct);
}