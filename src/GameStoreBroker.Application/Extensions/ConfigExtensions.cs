// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Extensions
{
    internal static class ConfigExtensions
    {
        public static GamePackageDate GetGamePackageDate(this MandatoryDateConfig mandatoryDateConfig) =>
            new ()
            {
                IsEnabled = mandatoryDateConfig is not null,
                EffectiveDate = mandatoryDateConfig?.MandatoryDate,
            };

        public static GamePackageDate GetGamePackageDate(this AvailabilityDateConfig availabilityDateConfig) =>
            new()
            {
                IsEnabled = availabilityDateConfig is not null,
                EffectiveDate = availabilityDateConfig?.AvailabilityDate,
            };

        public static GamePackageDates GetGamePackageDates(this ImportPackagesOperationConfig config) =>
            new()
            {
                MandatoryDate = config.MandatoryDateConfig.GetGamePackageDate(),
                AvailabilityDate = config.AvailabilityDateConfig.GetGamePackageDate(),
            };

        public static GamePackageDates GetGamePackageDates(this UploadUwpPackageOperationConfig config) =>
            new()
            {
                MandatoryDate = config.MandatoryDateConfig.GetGamePackageDate(),
                AvailabilityDate = config.AvailabilityDateConfig.GetGamePackageDate(),
            };
    }
}