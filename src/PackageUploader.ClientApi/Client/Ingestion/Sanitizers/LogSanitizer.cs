// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace PackageUploader.ClientApi.Client.Ingestion.Sanitizers;

public static class LogSanitizer
{
    public static string SanitizeJsonResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return jsonResponse;

        // Sanitizing token from responses
        var responseBody = Regex.Replace(jsonResponse, "\"token\":\\s*\"[^\"]+?([^\\/\"]+)\"", "\"token\":\"REDACTED\"");

        // Sanitizing File Sas Uri from responses
        var sasPropertyMatch = Regex.Match(responseBody, "\"fileSasUri\":\\s*\"[^\"]+?([^\\/\"]+)\"");
        if (sasPropertyMatch.Success)
            responseBody = responseBody.Replace(sasPropertyMatch.Groups[0].Value, sasPropertyMatch.Groups[0].Value.Split('?')[0] + "?REDACTED\"");

        return responseBody;
    }
}