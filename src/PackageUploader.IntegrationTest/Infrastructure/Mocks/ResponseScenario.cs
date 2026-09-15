// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.IntegrationTest.Infrastructure.Mocks;

/// <summary>How a stubbed endpoint should behave for a single, non-stateful response.</summary>
internal enum ResponseScenario
{
    /// <summary>Return a normal 2xx response with a valid body.</summary>
    Success,

    /// <summary>Return a 500 Internal Server Error.</summary>
    ServerError,

    /// <summary>Return a 401 Unauthorized.</summary>
    Unauthorized,

    /// <summary>Return a 404 Not Found.</summary>
    NotFound,
}
