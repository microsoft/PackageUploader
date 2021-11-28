// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Operations
{
    internal abstract class Operation
    {
        protected readonly ILogger _logger;

        protected Operation(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                _logger.LogDebug("GameStoreBroker is running.");
                await ProcessAsync(ct).ConfigureAwait(false);
                return 0;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Operation cancelled.");
                return 1;
            }
            catch (OptionsValidationException e)
            {
                _logger.LogError("The configuration file is not valid:");
                foreach (var failure in e.Failures)
                {
                    _logger.LogError(failure);
                }
                return 3;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogTrace(e, "Exception thrown.");
                return 3;
            }
            finally
            {
                _logger.LogInformation("GameStoreBroker has finished running.");
            }
        }

        protected abstract Task ProcessAsync(CancellationToken ct);
    }
}
