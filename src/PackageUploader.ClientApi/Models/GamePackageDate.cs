// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace PackageUploader.ClientApi.Models;

public class GamePackageDate
{
    public bool IsEnabled { get; set; }

    private string _effectiveDate = null!;
    public string EffectiveDate
    {
        get => _effectiveDate;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _effectiveDate = null;
                return;
            }

            // Do a parse here to ensure this value is valid up front rather than throwing an exception later when it's first
            // used. We avoid making EffectiveDate a DateTime? directly because the parse code will then throw an exception on
            // an empty string, which we'd rather treat as 'null'.
            _ = DateTime.Parse(value, CultureInfo.InvariantCulture);
            _effectiveDate = value;
        }
    }

    public DateTime? GetEffectiveDate()
    {
        if (string.IsNullOrWhiteSpace(EffectiveDate))
        {
            return null;
        }
        return GetUtcDateWithHour(DateTime.Parse(EffectiveDate, CultureInfo.InvariantCulture));
    }

    private static DateTime? GetUtcDateWithHour(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var input = dateTime.Value.ToUniversalTime();
        var output = new DateTime(input.Year, input.Month, input.Day, input.Hour, 0, 0, DateTimeKind.Utc);
        return output;
    }
}