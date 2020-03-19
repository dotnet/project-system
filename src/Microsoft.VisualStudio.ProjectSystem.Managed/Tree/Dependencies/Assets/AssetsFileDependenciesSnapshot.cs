// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using NuGet.Common;
using NuGet.ProjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets
{
    /// <summary>
    /// Immutable snapshot of data captured from <c>project.assets.json</c>.
    /// </summary>
    internal sealed class AssetsFileDependenciesSnapshot
    {
        private static readonly LockFileFormat s_lockFileFormat = new LockFileFormat();

        public static AssetsFileDependenciesSnapshot Empty { get; } = new AssetsFileDependenciesSnapshot(null);

        /// <summary>
        /// List of diagnostic messages included in the snapshot. May be empty.
        /// </summary>
        public ImmutableArray<AssetsFileLogMessage> Logs { get; }

        /// <summary>
        /// Produces an updated snapshot by reading the <c>project.assets.json</c> file at <paramref name="path"/>.
        /// If the file could not be read, or no changes are detected, the current snapshot (this) is returned.
        /// </summary>
        public AssetsFileDependenciesSnapshot UpdateFromAssetsFile(string? path)
        {
            try
            {
                // Parse the file
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096 * 10, FileOptions.SequentialScan);

                LockFile lockFile = s_lockFileFormat.Read(fileStream, path);

                return new AssetsFileDependenciesSnapshot(lockFile);
            }
            catch
            {
                return this;
            }
        }

        private AssetsFileDependenciesSnapshot(LockFile? lockFile)
        {
            if (lockFile == null || lockFile.LogMessages.Count == 0)
            {
                Logs = ImmutableArray<AssetsFileLogMessage>.Empty;
            }
            else
            {
                ImmutableArray<AssetsFileLogMessage>.Builder builder = ImmutableArray.CreateBuilder<AssetsFileLogMessage>(lockFile.LogMessages.Count);
                foreach (IAssetsLogMessage logMessage in lockFile.LogMessages)
                {
                    builder.Add(new AssetsFileLogMessage(logMessage));
                }
                Logs = builder.MoveToImmutable();
            }
        }
    }

    internal readonly struct AssetsFileLogMessage
    {
        public AssetsFileLogMessage(IAssetsLogMessage logMessage)
        {
            Code = logMessage.Code;
            Level = logMessage.Level;
            WarningLevel = logMessage.WarningLevel;
            Message = logMessage.Message;
            LibraryId = logMessage.LibraryId;
            TargetGraphs = logMessage.TargetGraphs;
        }

        public NuGetLogCode Code { get; }
        public NuGet.Common.LogLevel Level { get; }
        public WarningLevel WarningLevel { get; }
        public string Message { get; }
        public string LibraryId { get; }
        public IReadOnlyList<string> TargetGraphs { get; }
    }
}
