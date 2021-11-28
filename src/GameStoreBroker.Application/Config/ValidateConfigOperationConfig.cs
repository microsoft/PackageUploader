// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class ValidateConfigOperationConfig
    {
        [Required]
        public Operations.OperationName ValidateOperationName { get; set; }
    }
}
