// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectChangeDiffFactory
    {
        public static IProjectChangeDiff WithAddedItems(string semiColonSeparatedItems)
        {
            if (semiColonSeparatedItems.Length == 0)
                return WithNoChanges();

            return WithAddedItems(semiColonSeparatedItems.Split(';'));
        }

        public static IProjectChangeDiff WithAddedItems(params string[] addedItems)
        {
            return Create(addedItems: ImmutableHashSet.Create(StringComparers.Paths, addedItems));
        }

        public static IProjectChangeDiff WithRemovedItems(string semiColonSeparatedItems)
        {
            if (semiColonSeparatedItems.Length == 0)
                return WithNoChanges();

            return WithRemovedItems(semiColonSeparatedItems.Split(';'));
        }

        public static IProjectChangeDiff WithRemovedItems(params string[] removedItems)
        {
            return Create(removedItems: ImmutableHashSet.Create(StringComparers.Paths, removedItems));
        }

        public static IProjectChangeDiff WithNoChanges()
        {
            return new ProjectChangeDiff();
        }

        public static IProjectChangeDiff Create(IImmutableSet<string> addedItems = null, IImmutableSet<string> removedItems = null, IImmutableSet<string> changedItems = null)
        {
            return new ProjectChangeDiff(addedItems, removedItems, changedItems);
        }
    }
}
