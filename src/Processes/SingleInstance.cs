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

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace LibreccioWeather.Core.Processes;

public sealed class SingleInstance : IDisposable
{
    private const byte ActivationRequest = 1;
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromSeconds(5);

    private readonly string _pipeName;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _listener;
    private bool _disposed;

    public SingleInstance(string applicationName)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        string user = identity.User?.Value ?? throw new InvalidOperationException("The current Windows user has no security identifier.");
        _pipeName = applicationName + "." + user;

        // The handle marks the running process without acquiring thread-affine mutex ownership.
        // The global namespace also protects settings shared by this user across Windows sessions.
        _mutex = new Mutex(false, "Global\\" + _pipeName, out bool createdNew);
        IsFirstInstance = createdNew;
    }

    public bool IsFirstInstance { get; }

    public void StartListening(Action activate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsFirstInstance || _listener is not null)
        {
            throw new InvalidOperationException("Only the first instance can start one activation listener.");
        }

        _listener = ListenAsync(activate, _shutdown.Token);
    }

    public async Task<bool> ActivateExistingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ActivationTimeout);

        try
        {
            using NamedPipeClientStream pipe = new(
                ".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
            byte[] processIdBytes = new byte[sizeof(int)];
            await pipe.ReadExactlyAsync(processIdBytes, timeout.Token).ConfigureAwait(false);
            int processId = BinaryPrimitives.ReadInt32LittleEndian(processIdBytes);
            if (processId <= 0)
            {
                return false;
            }

            // A newly launched process can grant the existing window permission to take focus.
            AllowSetForegroundWindow((uint)processId);
            byte[] request = [ActivationRequest];
            await pipe.WriteAsync(request, timeout.Token).ConfigureAwait(false);
            await pipe.ReadExactlyAsync(request, timeout.Token).ConfigureAwait(false);
            return request[0] == ActivationRequest;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
        {
            Trace.TraceWarning("The running instance could not be activated: {0}", exception.Message);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();
        // Every listener await uses the shutdown token and never waits for the window's dispatcher.
        _listener?.GetAwaiter().GetResult();
        _shutdown.Dispose();
        _mutex.Dispose();
    }

    private async Task ListenAsync(Action activate, CancellationToken cancellationToken)
    {
        byte[] processIdBytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(processIdBytes, Environment.ProcessId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream pipe = new(
                    _pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                // A client that stops during activation must not prevent later launches from connecting.
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ActivationTimeout);
                await pipe.WriteAsync(processIdBytes, timeout.Token).ConfigureAwait(false);
                byte[] request = new byte[1];
                await pipe.ReadExactlyAsync(request, timeout.Token).ConfigureAwait(false);
                if (request[0] == ActivationRequest)
                {
                    activate();
                    await pipe.WriteAsync(request, timeout.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
            {
                Trace.TraceWarning("The activation request could not be handled: {0}", exception.Message);
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
