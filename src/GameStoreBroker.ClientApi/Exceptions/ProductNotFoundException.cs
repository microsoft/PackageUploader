// Copyright (C) Microsoft. All rights reserved.

using System;

namespace GameStoreBroker.ClientApi.Exceptions
{
    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException(string errorMessage, Exception innerException = null) : base(errorMessage, innerException)
        {
        }
    }
}