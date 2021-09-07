// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Schema
{
    internal class UploadGamePackageOperationSchema : UploadPackageOperationSchema
    {
        protected override string GetOperationName() => "UploadGamePackage";

        [Required]
        public GameAssets GameAssets { get; set; }
    }
}
