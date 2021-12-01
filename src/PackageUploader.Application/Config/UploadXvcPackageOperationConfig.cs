// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Operations;
using PackageUploader.ClientApi.Models;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Config
{
    internal class UploadXvcPackageOperationConfig : UploadPackageOperationConfig
    {
        internal override OperationName GetOperationName() => OperationName.UploadXvcPackage;

        [Required]
        public GameAssets GameAssets { get; set; }
    }
}
