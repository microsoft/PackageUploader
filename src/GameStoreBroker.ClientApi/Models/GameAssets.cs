// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.ClientApi.Models
{
    public class GameAssets
    {
        [Required(ErrorMessage = "PackageFilePath is required")]
        public string PackageFilePath { get; set; }

        public string EkbFilePath { get; set; }
        public string SubvalFilePath { get; set; }
        public string SymbolsFilePath { get; set; }
        public string DiscLayoutFilePath { get; set; }
    }
}