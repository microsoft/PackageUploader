using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static GameStoreBroker.ClientApi.IngestionModels.IngestionGameProduct;

namespace GameStoreBroker.Api.IngestionModels
{
    internal class IngestionPackageCreationRequest : IngestionGamePackage
    {
        /// <summary>
        /// The market group ID to put the package in
        /// </summary>
        public string MarketGroupId { get; set; }

        /// <summary>
        /// The package configuration Id
        /// </summary>
        public string PackageConfigurationId { get; set; }
    }
}