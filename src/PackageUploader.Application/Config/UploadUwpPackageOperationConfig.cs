// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Operations;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;

namespace PackageUploader.Application.Config
{
    internal class UploadUwpPackageOperationConfig : UploadPackageOperationConfig, IGameConfiguration
    {
        internal override OperationName GetOperationName() => OperationName.UploadUwpPackage;

        public GamePackageDate MandatoryDate { get; set; }
        public GameGradualRolloutInfo GradualRollout { get; set; }
    }
}
