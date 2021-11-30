// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class GenerateConfigTemplateOperationConfig
    {
        [Required]
        public OperationName OperationName { get; set; }

        public bool Overwrite { get; set; }
    }
}
