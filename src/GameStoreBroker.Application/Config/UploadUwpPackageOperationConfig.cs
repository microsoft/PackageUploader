// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.Application.Config
{
    internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig
    {
        internal override string GetOperationName() => "UploadUwpPackage";

        public MandatoryDateConfig MandatoryDateConfig { get; set; }
    }
}
