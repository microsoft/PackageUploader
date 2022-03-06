// Copyright (C) Microsoft. All rights reserved.

using PackageUploader.ClientApi.Client.Ingestion.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PackageUploader.Application.Models;

internal class Product
{
    /// <summary>
    /// Product Id, aka. LongId - looks like a long number
    /// </summary>
    public string ProductId { get; }

    /// <summary>
    /// Big Id, combination of letters/numbers usually beginning with 9
    /// </summary>
    public string BigId { get; }

    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// List of friendly names of all branches in the product
    /// </summary>
    public IList<string> BranchFriendlyNames { get; }

    /// <summary>
    /// List of names of all flights in the product
    /// </summary>
    public IList<string> FlightNames { get; }

    public Product(GameProduct gameProduct, IEnumerable<IGamePackageBranch> branches)
    {
        ProductId = gameProduct.ProductId;
        BigId = gameProduct.BigId;
        ProductName = gameProduct.ProductName;
        
        BranchFriendlyNames = new List<string>();
        FlightNames = new List<string>();
        foreach (var branch in branches)
        {
            if (branch.IsFlight)
            {
                FlightNames.Add(((GamePackageFlight)branch).FlightName);
            }
            else
            {
                BranchFriendlyNames.Add(branch.BranchFriendlyName);
            }
        }
    }

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ToJson()
    {
        try
        {
            var serializedObject = JsonSerializer.Serialize(this, DefaultJsonSerializerOptions);
            return serializedObject;
        }
        catch (Exception ex)
        {
            return $"Could not serialize product to json - {ex.Message}";
        }
    }
}
