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

using LibreccioWeather.Core.Processes;

namespace LibreccioWeather.Core.Tests.Processes;

public sealed class SingleInstanceTests
{
    [Fact]
    public async Task Constructor_ExistingInstance_ExcludesDuplicatesUntilHandlesClose()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string name = "Libreccio-Weather.Tests." + Guid.NewGuid().ToString("N");
        using (SingleInstance first = new(name))
        {
            using SingleInstance second = new(name);
            Assert.True(first.IsFirstInstance);
            Assert.False(second.IsFirstInstance);

            TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Thread disposingThread = new(() =>
            {
                try
                {
                    first.Dispose();
                    disposed.SetResult();
                }
                catch (Exception exception)
                {
                    disposed.SetException(exception);
                }
            });
            disposingThread.Start();
            await disposed.Task.WaitAsync(cancellationToken);

            using SingleInstance third = new(name);
            Assert.False(third.IsFirstInstance);
        }

        using SingleInstance reopened = new(name);
        Assert.True(reopened.IsFirstInstance);
    }

    [Fact]
    public async Task ActivateExistingAsync_RepeatedLaunches_NotifiesFirstInstance()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string name = "Libreccio-Weather.Tests." + Guid.NewGuid().ToString("N");
        using SingleInstance first = new(name);
        int activations = 0;
        first.StartListening(() => Interlocked.Increment(ref activations));

        using SingleInstance second = new(name);
        using SingleInstance third = new(name);
        Assert.False(second.IsFirstInstance);
        Assert.False(third.IsFirstInstance);
        Assert.True(await second.ActivateExistingAsync(cancellationToken));
        Assert.True(await third.ActivateExistingAsync(cancellationToken));
        Assert.Equal(2, Volatile.Read(ref activations));
    }
}
