// Copyright (C) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace GameStoreBroker.Application.Schema
{
    internal abstract class BaseOperationSchema
    {
        [JsonPropertyName("operationName")]
        public string OperationName { get; set; }
        
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }
        
        [JsonPropertyName("bigId")]
        public string BigId { get; set; }
        
        [JsonPropertyName("aadAuthInfo")]
        public AadAuthInfoSchema AadAuthInfo { get; set; }
    }
}
