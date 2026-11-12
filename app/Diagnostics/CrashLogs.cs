/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using LibreccioWeather.Updates;
using Microsoft.Win32;
using ApplicationIdentity = LibreccioWeather.Core.Configuration.ApplicationIdentity;

namespace LibreccioWeather.Diagnostics;

/// <summary>
/// Saves the errors of the app's log to a file, with the app and system versions, for sharing with the developers.
/// </summary>
public static partial class CrashLogs
{
    // The app's folder in the user's temporary files, which Windows may clear like any cache.
    public static string Path => System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), ApplicationIdentity.DataDirectoryName, "libreccioweather_crash_logs.txt");

    /// <summary>
    /// Writes the error entries of the previous and current logs, oldest first, followed by the debug information, to
    /// <see cref="Path"/>, replacing any earlier file. Returns the file's path.
    /// </summary>
    public static string Dump()
    {
        StringBuilder text = new();
        foreach (string log in new[] { ErrorLog.PreviousPath, ErrorLog.Path })
        {
            AppendErrors(text, log);
        }

        text.Append(GetDebugInfo());
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, text.ToString());
        return Path;
    }

    /// <summary>
    /// Opens the saved file with the program Windows associates with text files.
    /// </summary>
    public static void Open(string path)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            Trace.TraceWarning("Could not open {0}: {1}", path, exception.Message);
        }
    }

    public static string GetDebugInfo()
    {
        string build = AppVersion.IsDebug ? "debug" : "release";
        return $"""
            App version: {AppVersion.VersionName} ({build}, {AppVersion.CommitSha ?? "unknown commit"}
            Windows version: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})
            .NET version: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture} process)
            Device manufacturer: {BiosValue("SystemManufacturer")}
            Device model: {BiosValue("SystemProductName")}
            """;
    }

    // Only errors are kept, as a log of errors is what is shared. An entry runs from its start line to the next one,
    // so the lines of a stack trace stay with their error; lines before the first entry are skipped.
    private static void AppendErrors(StringBuilder text, string log)
    {
        if (!File.Exists(log))
        {
            return;
        }

        // The log is still open for writing while the app runs.
        using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        bool inError = false;
        while (reader.ReadLine() is { } line)
        {
            if (EntryStart().Match(line) is { Success: true } entry)
            {
                inError = entry.Groups[1].Value == "E";
            }

            if (inError)
            {
                text.AppendLine(line);
            }
        }
    }

    private static string BiosValue(string name)
    {
        return Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", name, null) as string ?? "unknown";
    }

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}[+-][0-9]{2}:[0-9]{2} ([EWID]) ")]
    private static partial Regex EntryStart();
}
