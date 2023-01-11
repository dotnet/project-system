// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

internal sealed partial class BuildUpToDateCheck
{
    /// <summary>
    ///     Aggregates details of file system operations that would occur during a project build.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     In some situations, the fast up-to-date check can identify that the only work needed for a build is
    ///     to copy and touch files on disk. For example, in the case of a project that is up-to-date other than
    ///     one or more <c>CopyToOutputDirectory="Always"</c> items.
    /// </para>
    /// <para>
    ///     The fast up-to-date check can aggregate information about these operations during its check. If no good
    ///     reason to build is found (such as an input being newer than an output), then VS may avoid calling
    ///     MSBuild and perform the copy directly. This can speed up builds by several orders of magnitude.
    /// </para>
    /// </remarks>
    internal sealed class FileSystemOperationAggregator
    {
        private readonly IFileSystem _fileSystem;
        private readonly Log _logger;

        private HashSet<(string Source, string Destination)>? _pendingCopies;

        /// <summary>
        /// Tracks whether the project might have benefitted from build acceleration.
        /// When the feature is not enabled, and acceleration might have helped, we will
        /// log a message to the user suggesting that they enable it.
        /// </summary>
        public bool IsAccelerationCandidate { get; internal set; }

        /// <summary>
        /// Gets whether build acceleration was enabled in any configuration or not.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   <list type="bullet">
        ///     <item><see langword="null"/> when no value was specified in any configuration.</item>
        ///     <item><see langword="false"/> if all configurations had it disabled.</item>
        ///     <item><see langword="true"/> if any configuration had it enabled.</item>
        ///   </list>
        ///   </para>
        ///   <para>
        ///     When no value is specified (value <see langword="null"/>), build acceleration should
        ///     be considered disabled (the default behavior). Projects must opt in to this feature.
        ///   </para>
        ///   <para>
        ///     This value is set at the configured level, though We expect all configurations
        ///     to share the same value, so expose it at this top level.
        ///   </para>
        /// </remarks>
        public bool? IsAccelerationEnabled { get; internal set; }

        public BuildAccelerationResult AccelerationResult
        {
            get
            {
                if (IsAccelerationEnabled is true)
                {
                    if (_pendingCopies is not null)
                        return BuildAccelerationResult.EnabledAccelerated;

                    return BuildAccelerationResult.EnabledNotAccelerated;
                }

                if (IsAccelerationCandidate)
                    return BuildAccelerationResult.DisabledCandidate;

                return BuildAccelerationResult.DisabledNotCandidate;
            }
        }

        public FileSystemOperationAggregator(IFileSystem fileSystem, Log logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public void AddCopy(string source, string destination)
        {
            System.Diagnostics.Debug.Assert(Path.IsPathRooted(source), "Source path should be rooted.");
            System.Diagnostics.Debug.Assert(Path.IsPathRooted(destination), "Destination path should be rooted.");
            System.Diagnostics.Debug.Assert(!StringComparers.Paths.Equals(source, destination), "Destination and path should not be equal.");

            _pendingCopies ??= new();
            _pendingCopies.Add((source, destination));

            _logger.Verbose(nameof(Resources.FUTD_RememberingCopiedFile_2), source, destination);
        }

        /// <summary>
        /// Applies any pending file system operations that were aggregated in this object.
        /// </summary>
        /// <returns>
        /// A tuple of:
        /// <list type="number">
        ///   <item>
        ///     <c>Success</c> indicating whether all required operations were applied successfully.
        ///     Will be <see langword="true"/> even if <c>CopyCount</c> and <c>TouchCount</c> were zero.
        ///     If <see langword="false"/>, then <see cref="Log.Fail(string, string, object[])"/> will have been called.
        ///   </item>
        ///   <item>
        ///     <c>CopyCount</c> the number of files that were copied.
        ///   </item>
        /// </list>
        /// </returns>
        public (bool Success, int CopyCount) TryApplyFileSystemOperations()
        {
            int copyCount = 0;

            if (_pendingCopies is not null)
            {
                // we have some copies to perform

                _logger.Info(nameof(Resources.FUTD_CopyingFilesToAccelerateBuild_1), _pendingCopies.Count);

                using Log.Scope _ = _logger.IndentScope();

                foreach ((string source, string destination) in _pendingCopies)
                {
                    try
                    {
                        (long SizeBytes, DateTime WriteTimeUtc)? sourceInfo = _fileSystem.GetFileSizeAndWriteTimeUtc(source);
                        (long SizeBytes, DateTime WriteTimeUtc)? destinationInfo = _fileSystem.GetFileSizeAndWriteTimeUtc(destination);

                        if (sourceInfo is null)
                        {
                            // We ensure the source file exists during the scan, so it should rarely not exist at
                            // this point, and then only as a result of a race condition.
                            _logger.Info(nameof(Resources.FUTD_CheckingCopyFileSourceNotFound_2), source, destination);

                            return (Success: false, CopyCount: copyCount);
                        }

                        // TODO compare MVID here to avoid copying identical assemblies over each other even if they have different timestamps (can happen with deterministic builds)

                        if (destinationInfo is null || sourceInfo.Value.SizeBytes != destinationInfo.Value.SizeBytes || sourceInfo.Value.WriteTimeUtc != destinationInfo.Value.WriteTimeUtc)
                        {
                            _logger.Info(nameof(Resources.FUTD_FromTo_2), source, destination);

                            if (destinationInfo is null)
                            {
                                // Ensure the destination directory actually exists on disk
                                _fileSystem.CreateDirectory(Path.GetDirectoryName(destination));
                            }

                            // TODO add retry logic in case of failed copies? MSBuild does this with CopyRetryCount and CopyRetryDelayMilliseconds

                            // Copy the file
                            _fileSystem.CopyFile(source, destination, overwrite: true);

                            copyCount++;
                        }
                        else
                        {
                            _logger.Verbose(nameof(Resources.FUTD_SkippingCopyDueToIdenticalSizeAndWriteTime_2), source, destination);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Fail("ExceptionCopyingFile", nameof(Resources.FUTD_ExceptionCopyingFile_1), ex);
                        return (Success: false, CopyCount: copyCount);
                    }
                }
            }

            return (Success: true, CopyCount: copyCount);
        }
    }

    /// <summary>
    /// Wraps a parent <see cref="FileSystemOperationAggregator"/> for a specific configuration.
    /// </summary>
    /// <remarks>
    /// We model the enabled state of this feature per-configuration. While we expect this
    /// option to be unconfigured, modelling it this way allows us to respect a per-configuration
    /// preference.
    /// </remarks>
    private sealed class ConfiguredFileSystemOperationAggregator
    {
        private readonly FileSystemOperationAggregator _parent;
        private readonly bool? _isBuildAccelerationEnabled;

        public ConfiguredFileSystemOperationAggregator(FileSystemOperationAggregator parent, bool? isBuildAccelerationEnabled)
        {
            _parent = parent;
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            if (isBuildAccelerationEnabled is true)
            {
                // True if any configuration is enabled
                _parent.IsAccelerationEnabled = true;
            }
            else if (_parent.IsAccelerationEnabled is null && isBuildAccelerationEnabled is false)
            {
                // Note an explicit disable
                _parent.IsAccelerationEnabled = false;
            }
        }

        public bool AddCopy(string source, string destination)
        {
            if (_isBuildAccelerationEnabled is true)
            {
                _parent.AddCopy(source, destination);
                return true;
            }
            else if (_isBuildAccelerationEnabled is null)
            {
                _parent.IsAccelerationCandidate = true;
            }

            return false;
        }
    }
}
