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
using System.IO;
using ApplicationIdentity = LibreccioWeather.Core.Configuration.ApplicationIdentity;

namespace LibreccioWeather.Diagnostics;

/// <summary>
/// Writes trace output and unhandled exceptions to a file in the app's data directory, where a published build keeps
/// a record of what went wrong.
/// </summary>
public static class ErrorLog
{
    // Past this size the log becomes the previous log at the next start, so the file never grows without bound and
    // the log of a session that crashed survives the next start.
    private const long MaxLength = 1024 * 1024;

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationIdentity.DataDirectoryName,
        "libreccio-weather.log");

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
                file.MoveTo(System.IO.Path.ChangeExtension(Path, ".old.log"), overwrite: true);
            }

            var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            Trace.Listeners.Add(new TextWriterTraceListener(new StreamWriter(stream) { AutoFlush = true })
            {
                TraceOutputOptions = TraceOptions.DateTime,
            });
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
}
