// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.Application.Config;
using System.ComponentModel.DataAnnotations;

namespace PackageUploader.Application.Test.Config;

internal class TestGetProductOperationConfig : GetProductOperationConfig;

internal class TestGetPackagesOperationConfig : GetPackagesOperationConfig;

internal class TestRemovePackagesOperationConfig : RemovePackagesOperationConfig;

internal class TestUploadUwpPackageOperationConfig : UploadUwpPackageOperationConfig;

internal class TestUploadXvcPackageOperationConfig : UploadXvcPackageOperationConfig;

internal class TestImportPackagesOperationConfig : ImportPackagesOperationConfig;

internal static class ConfigTestHelper
{
    public static List<ValidationResult> ValidateConfig(IValidatableObject config)
    {
        var results = new List<ValidationResult>();
        results.AddRange(config.Validate(new ValidationContext(config)));
        return results;
    }
}
