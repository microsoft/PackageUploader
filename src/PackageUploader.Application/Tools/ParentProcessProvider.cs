// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PackageUploader.Application.Tools;

/// <inheritdoc cref="IParentProcessProvider"/>
/// <remarks>
/// Windows implementation. The .NET base class library exposes no parent-process API, so this reads the
/// parent process id out of the current process's own basic information block via
/// <c>NtQueryInformationProcess</c>, then resolves that id to a name.
///
/// The query targets the CURRENT process pseudo-handle, so it needs no additional access rights and cannot
/// fail for permissions reasons. Only the subsequent name lookup can be denied, and that is tolerated.
///
/// Deliberately free of struct marshalling: the buffer is read field-by-field through <see cref="Marshal"/>
/// so the whole path stays blittable and safe under <c>PublishAot</c>, which this project enables.
///
/// Every failure mode returns null rather than throwing, per the interface contract.
/// </remarks>
internal sealed class ParentProcessProvider : IParentProcessProvider
{
    /// <summary>ProcessBasicInformation, the <c>PROCESSINFOCLASS</c> value for the query below.</summary>
    private const int ProcessBasicInformation = 0;

    /// <summary>
    /// PROCESS_BASIC_INFORMATION is six pointer-sized fields: ExitStatus, PebBaseAddress, AffinityMask,
    /// BasePriority, UniqueProcessId, InheritedFromUniqueProcessId. The parent id is the last of them.
    /// (ExitStatus and BasePriority are 32-bit in C, but each is padded to pointer alignment.)
    /// </summary>
    private const int PointerFieldCount = 6;
    private const int InheritedFromUniqueProcessIdFieldIndex = 5;

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        IntPtr processInformation,
        int processInformationLength,
        IntPtr returnLength);

    public string GetParentProcessFileName()
    {
        try
        {
            // Non-Windows hosts have no equivalent lookup here, so the parent is reported as unknown.
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            using var current = Process.GetCurrentProcess();

            if (!TryGetParentProcessId(out var parentProcessId))
            {
                return null;
            }

            using var parent = Process.GetProcessById(parentProcessId);

            // Process ids are recycled. If the process now holding our recorded parent id started after we
            // did, it cannot be our real parent, so report unknown rather than an unrelated executable.
            if (StartedAfter(parent, current))
            {
                return null;
            }

            return GetFileName(parent);
        }
        catch (Exception)
        {
            // Contract: never throw. The parent process may have exited between the two calls above
            // (ArgumentException), or the OS may refuse the lookup (Win32Exception). Either way the caller
            // treats a null as "unknown parent" and falls back to the other recursion barriers.
            return null;
        }
    }

    private static bool TryGetParentProcessId(out int parentProcessId)
    {
        parentProcessId = 0;

        var bufferLength = PointerFieldCount * IntPtr.Size;
        var buffer = Marshal.AllocHGlobal(bufferLength);

        try
        {
            // -1 is the pseudo-handle for the current process; it needs no rights and needs no closing.
            var status = NtQueryInformationProcess(
                new IntPtr(-1),
                ProcessBasicInformation,
                buffer,
                bufferLength,
                IntPtr.Zero);

            if (status != 0)
            {
                return false;
            }

            var parent = Marshal.ReadIntPtr(buffer, InheritedFromUniqueProcessIdFieldIndex * IntPtr.Size);

            if (parent == IntPtr.Zero)
            {
                return false;
            }

            parentProcessId = (int)parent;
            return parentProcessId > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// True only when both start times are readable and the parent's is the later one. An unreadable start
    /// time leaves the process id recycling check inconclusive, which is treated as "not recycled" so a
    /// genuine MakePkg.exe parent is still recognized.
    /// </summary>
    private static bool StartedAfter(Process parent, Process current)
    {
        try
        {
            return parent.StartTime > current.StartTime;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Prefers the module name because it carries the extension (<c>MakePkg.exe</c>). Reading another
    /// process's main module needs rights that are often unavailable across elevation or bitness
    /// boundaries, so it falls back to the extension-less process name, which callers accommodate.
    /// </summary>
    private static string GetFileName(Process parent)
    {
        try
        {
            var moduleName = parent.MainModule?.ModuleName;

            if (!string.IsNullOrWhiteSpace(moduleName))
            {
                return moduleName;
            }
        }
        catch (Exception)
        {
            // Fall through to the process name below.
        }

        return parent.ProcessName;
    }
}
