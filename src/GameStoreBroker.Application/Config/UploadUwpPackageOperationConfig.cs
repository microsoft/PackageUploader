// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Config
{
    internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig
    {
        internal override string GetOperationName() => "UploadUwpPackage";

        public GamePackageDate MandatoryDate { get; set; }
    }
}
