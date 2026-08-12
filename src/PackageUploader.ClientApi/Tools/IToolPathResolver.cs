// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.ClientApi.Tools;

/// <summary>
/// Locates a tool that ships alongside the application or with the Microsoft GDK.
/// </summary>
/// <remarks>
/// This is the single discovery implementation shared by every host, so the command line and the
/// desktop app can never disagree about which copy of a tool they will run.
/// <para>
/// Contract: implementations report "not found" by returning <see langword="null"/>, never by
/// throwing. An unreadable directory, a denied registry key, or a malformed PATH entry is skipped
/// and the search continues.
/// </para>
/// </remarks>
public interface IToolPathResolver
{
    /// <summary>
    /// Searches for <paramref name="fileName"/> in the application directory, the current directory,
    /// the <c>bin</c> directory of any installed GDK, and finally each directory on PATH, in that order.
    /// </summary>
    /// <param name="fileName">
    /// A bare file name such as <c>MakePkg.exe</c>, not a path. The search is by exact name.
    /// </param>
    /// <returns>
    /// The full path to the first match, or <see langword="null"/> when the file was not found in any
    /// searched location. Never throws.
    /// </returns>
    string Find(string fileName);
}
