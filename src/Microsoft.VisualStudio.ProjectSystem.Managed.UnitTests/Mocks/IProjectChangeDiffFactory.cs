// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public static IProjectChangeDiff WithRenameItems(string semiColonSeparatedOriginalNames, string semiColonSeparatedNewNames)
        {
            string[] originalNames = semiColonSeparatedOriginalNames.Split(';');
            string[] newNames = semiColonSeparatedNewNames.Split(';');

            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < originalNames.Length; i++)
            {
                builder.Add(originalNames[i], newNames[i]);
            }

            return Create(renamedItems: builder.ToImmutable());
        }

        public static IProjectChangeDiff WithNoChanges()
        {
            return new ProjectChangeDiff();
        }

        public static IProjectChangeDiff Create(IImmutableSet<string> addedItems = null, IImmutableSet<string> removedItems = null, IImmutableSet<string> changedItems = null, IImmutableDictionary<string, string> renamedItems = null)
        {
            return new ProjectChangeDiff(addedItems, removedItems, changedItems, renamedItems);
        }
    }
}
