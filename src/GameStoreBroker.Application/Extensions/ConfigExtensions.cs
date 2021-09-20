// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.ClientApi.Models;

namespace GameStoreBroker.Application.Extensions
{
    internal static class ConfigExtensions
    {
        public static GameConfiguration GetGameConfiguration(this ImportPackagesOperationConfig config) =>
            new()
            {
                MandatoryDate = config.MandatoryDate,
                AvailabilityDate = config.AvailabilityDate,
            };

        public static GameConfiguration GetGameConfiguration(this UploadUwpPackageOperationConfig config) =>
            new()
            {
                MandatoryDate = config.MandatoryDate,
                AvailabilityDate = config.AvailabilityDate,
            };
    }
}
