// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectRuleSnapshotsFactory
    {
        public static IImmutableDictionary<string, IProjectRuleSnapshot> Create()
        {
            return ImmutableStringDictionary<IProjectRuleSnapshot>.EmptyOrdinal;
        }

        public static IImmutableDictionary<string, IProjectRuleSnapshot> Add(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, string propertyValue)
        {
            if (!snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot snapshot))
            {
                snapshot = IProjectRuleSnapshotFactory.Create(ruleName, propertyName, propertyValue);
                return snapshots.Add(ruleName, snapshot);
            }

            return snapshots.SetItem(ruleName, snapshot.Add(propertyName, propertyValue));
        }
    }
}
