// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace PackageUploader.ClientApi.Client.Ingestion.Sanitizers;

public static partial class LogSanitizer
{
    [GeneratedRegex("\"token\":\\s*\"[^\"]+?([^\\/\"]+)\"")]
    private static partial Regex ResponseRegex();

    [GeneratedRegex("\"fileSasUri\":\\s*\"[^\"]+?([^\\/\"]+)\"")]
    private static partial Regex SasRegex();

    public static string SanitizeJsonResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return jsonResponse;

        // Sanitizing token from responses
        var responseBody = ResponseRegex().Replace(jsonResponse, "\"token\":\"REDACTED\"");

        // Sanitizing File Sas Uri from responses
        var sasPropertyMatch = SasRegex().Match(responseBody);
        if (sasPropertyMatch.Success)
            responseBody = responseBody.Replace(sasPropertyMatch.Groups[0].Value, sasPropertyMatch.Groups[0].Value.Split('?')[0] + "?REDACTED\"");

        return responseBody;
    }
}