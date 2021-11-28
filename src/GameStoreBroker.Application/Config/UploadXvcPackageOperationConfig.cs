// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Config
{
    internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
    {
        internal override Operations.OperationName GetOperationName() => Operations.OperationName.UploadXvcPackage;

        [Required]
        public GameAssets GameAssets { get; set; }
    }
}
