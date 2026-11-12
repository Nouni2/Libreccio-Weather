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

using System.Diagnostics;
using System.Globalization;
using System.IO;
using ApplicationIdentity = LibreccioWeather.Core.Configuration.ApplicationIdentity;

namespace LibreccioWeather.Diagnostics;

/// <summary>
/// Writes trace output and unhandled exceptions to a file in the app's data directory, where a published build keeps
/// a record of what went wrong.
/// </summary>
/// <remarks>
/// Each entry starts on a new line with its time and level, such as "2026-11-11T09:30:00.000+01:00 E ", followed by
/// its text, which can run over several lines. <see cref="CrashLogs"/> reads entries back by that start.
/// </remarks>
public static class ErrorLog
{
    // Past this size the log becomes the previous log at the next start, so the file never grows without bound and
    // the log of a session that crashed survives the next start.
    private const long MaxLength = 1024 * 1024;

    public const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationIdentity.DataDirectoryName,
        "libreccio-weather.log");

    public static string PreviousPath => System.IO.Path.ChangeExtension(Path, ".old.log");

    /// <summary>
    /// Starts writing trace output to the log. A log that cannot be opened leaves the app running without one.
    /// </summary>
    public static void Start()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var file = new FileInfo(Path);
            if (file.Exists && file.Length > MaxLength)
            {
                file.MoveTo(PreviousPath, overwrite: true);
            }

            var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            Trace.Listeners.Add(new LogFileListener(new StreamWriter(stream) { AutoFlush = true }));
            Trace.AutoFlush = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("The error log could not be opened: {0}", exception.Message);
        }
    }

    public static void Record(string source, Exception exception)
    {
        Trace.TraceError("{0}: {1}", source, exception);
    }

    // The usual log priority letters: E for errors, W for warnings, I for information, D for anything written
    // without a level.
    private sealed class LogFileListener(StreamWriter writer) : TraceListener
    {
        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
        {
            WriteEntry(eventType switch
            {
                TraceEventType.Critical or TraceEventType.Error => 'E',
                TraceEventType.Warning => 'W',
                _ => 'I',
            }, message ?? "");
        }

        public override void TraceEvent(
            TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
        {
            TraceEvent(eventCache, source, eventType, id,
                args is { Length: > 0 } && format is not null ? string.Format(CultureInfo.InvariantCulture, format, args) : format);
        }

        public override void Write(string? message)
        {
            WriteEntry('D', message ?? "");
        }

        public override void WriteLine(string? message)
        {
            WriteEntry('D', message ?? "");
        }

        public override void Flush()
        {
            writer.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                writer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void WriteEntry(char level, string message)
        {
            writer.WriteLine(DateTimeOffset.Now.ToString(TimeFormat, CultureInfo.InvariantCulture) + " " + level + " " + message);
        }
    }
}
