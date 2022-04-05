// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
