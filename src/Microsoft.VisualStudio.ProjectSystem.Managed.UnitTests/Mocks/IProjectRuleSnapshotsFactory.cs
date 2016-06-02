// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Moq;
using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectRuleSnapshotsFactory
    {
        public static IImmutableDictionary<string, IProjectRuleSnapshot> Create()
        {
            return ImmutableDictionary<string, IProjectRuleSnapshot>.Empty;
        }

        public static IImmutableDictionary<string, IProjectRuleSnapshot> Add(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, string propertyValue)
        {
            IProjectRuleSnapshot snapshot;
            if (!snapshots.TryGetValue(ruleName, out snapshot))
            {
                snapshot = IProjectRuleSnapshotFactory.Create(ruleName, propertyName, propertyValue);
                return snapshots.Add(ruleName, snapshot);
            }

            return snapshots.SetItem(ruleName, snapshot.Add(propertyName, propertyValue));
        }
    }
}
