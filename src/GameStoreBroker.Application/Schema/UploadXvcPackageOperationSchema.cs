// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Schema
{
    internal class UploadXvcPackageOperationSchema : UploadPackageOperationSchema
    {
        internal override string GetOperationName() => "UploadXvcPackage";

        [Required]
        public GameAssets GameAssets { get; set; }
    }
}
