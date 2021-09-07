// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Schema
{
    internal class UploadUwpPackageOperationSchema : UploadPackageOperationSchema
    {
        protected override string GetOperationName() => "UploadUwpPackage";

        [Required(ErrorMessage = "PackageFilePath is required")]
        public string PackageFilePath { get; set; }
    }
}
