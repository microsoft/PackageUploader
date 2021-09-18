// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace GameStoreBroker.ClientApi.Models
{
    public class GamePackageDate
    {
        public bool IsEnabled { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }
}