// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;

namespace GameStoreBroker.ClientApi.Client.Xfus.Exceptions
{
    public sealed class XfusServerException : Exception
    {
        private readonly TimeSpan? _retryAfter;

        public HttpStatusCode HttpStatusCode { get; }

        public TimeSpan RetryAfter => _retryAfter ?? default;

        public bool IsRetryable { get; }

        public XfusServerException()
        {
        }

        public XfusServerException(string message) : base(message)
        {
        }

        public XfusServerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public XfusServerException(HttpStatusCode statusCode, string message) : base($"StatusCode:{statusCode} Message:{message}")
        {
            HttpStatusCode = statusCode;
        }

        public XfusServerException(HttpStatusCode statusCode, TimeSpan retryAfter, string message) : base($"StatusCode:{statusCode} RetryAfter:{retryAfter} Message:{message}")
        {
            HttpStatusCode = statusCode;
            _retryAfter = retryAfter;
            IsRetryable = true;
        }
    }
}
