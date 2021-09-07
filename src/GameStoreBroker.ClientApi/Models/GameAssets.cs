// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace GameStoreBroker.ClientApi.Models
{
    public class GameAssets
    {
        [Required]
        public string PackageFilePath { get; set; }

        public string EkbFilePath { get; set; }
        public string SubValFilePath { get; set; }
        public string SymbolsFilePath { get; set; }
        public string DiscLayoutFilePath { get; set; }
    }
}