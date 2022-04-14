// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace PackageUploader.ClientApi.Client.Ingestion.Models;

public sealed class GameSubmission
{
    /// <summary>
    /// Resource ID
    /// </summary>
    public string Id { get; internal init; }

    /// <summary>
    /// State of the submission
    /// </summary>
    public GameSubmissionState GameSubmissionState { get; internal init; }

    /// <summary>
    /// Release number
    /// </summary>
    public int ReleaseNumber { get; internal init; }

    /// <summary>
    /// Friendly name
    /// </summary>
    public string FriendlyName { get; internal init; }

    /// <summary>
    /// Published time (UTC)
    /// </summary>
    public DateTime? PublishedTimeInUtc { get; internal init; }

    /// <summary>
    /// Submission validation items
    /// </summary>
    public List<GameSubmissionValidationItem> SubmissionValidationItems { get; internal set; }

    /// <summary>
    /// Submission publish details
    /// </summary>
    public GameSubmissionOptions SubmissionOption { get; internal init; }
}