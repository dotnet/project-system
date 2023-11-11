// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

[Export(typeof(IPackageRestoreCycleDetector))]
[method: ImportingConstructor]
internal sealed class PackageRestoreCycleDetector(
    UnconfiguredProject unconfiguredProject,
    ITelemetryService telemetryService,
    INonModalNotificationService userNotificationService)
    : IPackageRestoreCycleDetector
{
    // This algorithm is to detect the pattern A -> B -> A -> B -> A in the most recent N values.
    //
    // To keep track of the most N recent values we are using a queue of fixed size, where:
    //
    // - The most recent value is inserted at the front in O(1) time, and the oldest value is removed at the
    //   back in O(1) time.
    // - The counter will keep track of how many times values have appeared in the queue consecutively.
    //
    // i.e.
    //   A -> B -> C -> A -> B -> D -> X -> Y -> X -> Y -> X -> Y
    //                                |------cycle detected------|

    /// <summary>
    ///     Fixed size of the numbers of values to store.
    /// </summary>
    /// <remarks>
    ///     This represents how deep to search for hash cycle.
    /// </remarks>
    private const int Size = 6;

    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();
    private readonly Queue<Hash> _values = new(capacity: Size);
    private readonly Dictionary<Hash, int> _lookupTable = new(capacity: Size);

    private readonly UnconfiguredProject _unconfiguredProject = unconfiguredProject;
    private readonly ITelemetryService _telemetryService = telemetryService;
    private readonly INonModalNotificationService _userNotificationService = userNotificationService;

    private Hash? _lastHash;
    private int _nuGetRestoreSuccesses;
    private int _nuGetRestoreCyclesDetected;
    private int _counter;

    public async Task<bool> IsCycleDetectedAsync(Hash hash, CancellationToken cancellationToken)
    {
        // Ensure we have a stopwatch running throughout restores. We will stop it when we either
        // have two consecutive restores at the same hash, or when we detect a cycle.
        _stopwatch.Start();

        if (IsCycleDetected())
        {
            await OnCycleDetectedAsync();

            return true;
        }

        return false;

        bool IsCycleDetected()
        {
            lock (_lock)
            {
                if (_lastHash?.Equals(hash) == true)
                {
                    _nuGetRestoreSuccesses++;
                    Reset();
                    return false;
                }

                _lastHash = hash;

                if (_lookupTable.TryGetValue(hash, out int hashCounter) && hashCounter > 0)
                {
                    _counter++;

                    // Verify that a hash has repeated in almost all cases
                    if (_counter >= Size)
                    {
                        return true;
                    }
                }
                else
                {
                    _counter = 0;
                }

                if (_values.Count >= Size)
                {
                    Hash oldestHash = _values.Dequeue();
                    _lookupTable[oldestHash]--;
                }

                _values.Enqueue(hash);

                if (_lookupTable.TryGetValue(hash, out int count))
                {
                    _lookupTable[hash] = count + 1;
                }
                else
                {
                    _lookupTable.Add(hash, 1);
                }
            }

            return false;
        }

        async Task OnCycleDetectedAsync()
        {
            _stopwatch.Stop();

            _nuGetRestoreCyclesDetected++;

            // Send telemetry.
            _telemetryService.PostProperties(TelemetryEventName.NuGetRestoreCycleDetected, new[]
            {
                (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreDurationMillis, (object)_stopwatch.Elapsed.TotalMilliseconds),
                (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreSuccesses, _nuGetRestoreSuccesses),
                (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreCyclesDetected, _nuGetRestoreCyclesDetected)
            });

            // Notify the user.
            await _userNotificationService.ShowErrorAsync(
                message: string.Format(Resources.Restore_NuGetCycleDetected, _unconfiguredProject.FullPath),
                cancellationToken);

            // Clear out any internal state so that we start fresh on the next project change.
            // There's a chance the underlying issue will be resolved when we try again, and
            // we want to give it a chance.
            Reset();
        }

        void Reset()
        {
            _stopwatch.Reset();
            _counter = 0;
            _values.Clear();
            _lookupTable.Clear();
        }
    }
}
