// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class GenerateConfigTemplateOperationConfig
    {
        [Required]
        public string GenerateConfigTemplateOperationName { get; set; }
        public bool Overwrite { get; set; }
    }
}
