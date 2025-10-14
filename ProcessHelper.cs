using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

public static class ProcessHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    // Windows-specific native method
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref PROCESS_BASIC_INFORMATION pbi,
        int processInformationLength,
        out int returnLength
    );

    /// <summary>
    /// Return the parent Process of the current process.
    /// </summary>
    public static Process GetParentProcess()
    {
        return GetParentProcess(Process.GetCurrentProcess().Id);
    }

    /// <summary>
    /// Return the parent Process of a given process id.
    /// </summary>
    public static Process GetParentProcess(int pid)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetParentProcessWindows(pid);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetParentProcessLinux(pid);
        }
        else
        {
            // Either handle macOS or throw if you do not support it
            throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
        }
    }

    /// <summary>
    /// Windows-specific implementation using ntdll.dll
    /// </summary>
    private static Process GetParentProcessWindows(int pid)
    {
        // Retrieve handle to the target process
        var process = Process.GetProcessById(pid);
        IntPtr handle = process.Handle;

        // Use NtQueryInformationProcess
        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
        int returnLength;
        int status = NtQueryInformationProcess(
            handle,
            0,
            ref pbi,
            Marshal.SizeOf(pbi),
            out returnLength
        );

        if (status != 0)
        {
            // If the call failed, throw a Win32Exception with that status code
            throw new Win32Exception(status);
        }

        int parentPid = pbi.InheritedFromUniqueProcessId.ToInt32();
        // It's possible the parent no longer exists, so handle that
        try
        {
            return Process.GetProcessById(parentPid);
        }
        catch (ArgumentException)
        {
            // Parent not found
            return null;
        }
    }

    /// <summary>
    /// Linux-specific implementation by reading /proc/[pid]/status
    /// </summary>
    private static Process GetParentProcessLinux(int pid)
    {
        try
        {
            // For example, read the lines in /proc/[pid]/status
            var lines = File.ReadAllLines($"/proc/{pid}/status");
            foreach (var line in lines)
            {
                // Look for a line that starts with "PPid:"
                if (line.StartsWith("PPid:"))
                {
                    // The rest of the line contains the parent PID
                    string ppidString = line.Substring("PPid:".Length).Trim();
                    if (int.TryParse(ppidString, out int parentPid) && parentPid > 0)
                    {
                        return Process.GetProcessById(parentPid);
                    }
                }
            }
        }
        catch (IOException)
        {
            // For instance, the process may have exited or we may not have /proc
        }
        catch (UnauthorizedAccessException)
        {
            // In case of permission issues
        }

        // If anything fails or the parent wasn't found, return null
        return null;
    }
}
