// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Schema
{
    internal class UploadGamePackageOperationSchema : UploadPackageOperationSchema
    {
        public override string GetOperationName() => "UploadGamePackage";

        [Required(ErrorMessage = "GameAssets is required")]
        public GameAssets GameAssets { get; set; }
    }
}
