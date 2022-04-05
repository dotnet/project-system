// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a concrete implementation of <see cref="IProjectChangeDiff"/>
    /// </summary>
    internal class ProjectChangeDiff : IProjectChangeDiff
    {
        public ProjectChangeDiff(
            IImmutableSet<string>? addedItems = null,
            IImmutableSet<string>? removedItems = null,
            IImmutableSet<string>? changedItems = null,
            IImmutableDictionary<string, string>? renamedItems = null)
        {
            AddedItems = addedItems ?? ImmutableStringHashSet.EmptyOrdinal;
            RemovedItems = removedItems ?? ImmutableStringHashSet.EmptyOrdinal;
            ChangedItems = changedItems ?? ImmutableStringHashSet.EmptyOrdinal;
            RenamedItems = renamedItems ?? ImmutableStringDictionary<string>.EmptyOrdinal;
        }

        public IImmutableSet<string> AddedItems { get; }

        public IImmutableSet<string> RemovedItems { get; }

        public IImmutableSet<string> ChangedItems { get; }

        public IImmutableDictionary<string, string> RenamedItems { get; }

        public IImmutableSet<string> ChangedProperties
        {
            get { return ImmutableStringHashSet.EmptyOrdinal; }
        }

        public bool AnyChanges
        {
            get { return AddedItems.Count > 0 || RemovedItems.Count > 0 || ChangedItems.Count > 0 || RenamedItems.Count > 0; }
        }
    }
}
