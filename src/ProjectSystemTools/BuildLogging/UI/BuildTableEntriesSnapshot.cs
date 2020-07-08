// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal sealed class BuildTableEntriesSnapshot : WpfTableEntriesSnapshotBase
    {
        private readonly ImmutableList<Model.Build> _builds;

        public override int VersionNumber { get; }

        public override int Count => _builds.Count;

        public BuildTableEntriesSnapshot(ImmutableList<Model.Build> builds, int versionNumber)
        {
            _builds = builds;
            VersionNumber = versionNumber;
        }

        // We only add items to the end of our list, and we never reorder.
        // As such, any index in us will map to the same index in any newer snapshot.
        public override int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot) => currentIndex;

        public override bool TryGetValue(int index, string keyName, out object content) => _builds[index].TryGetValue(keyName, out content);
    }
}
