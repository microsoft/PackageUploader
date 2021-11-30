// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;
using GameStoreBroker.ClientApi.Models;
using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.UploadXvcPackage;

        [Required]
        public GameAssets GameAssets { get; set; }
    }
}
