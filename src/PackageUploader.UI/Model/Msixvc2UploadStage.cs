// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.Model;

public enum Msixvc2UploadStage
{
    NotStarted,
    Preparing,
    Uploading,
    Validating,
    Finalizing,
    Done
}
