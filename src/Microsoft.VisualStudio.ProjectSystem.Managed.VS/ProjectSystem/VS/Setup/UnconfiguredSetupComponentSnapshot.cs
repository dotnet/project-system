// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Immutable snapshot of the components required by an unconfigured project.
/// </summary>
internal sealed record class UnconfiguredSetupComponentSnapshot(ImmutableHashSet<string> ComponentIds)
{
    public static bool TryUpdate([NotNull] ref UnconfiguredSetupComponentSnapshot? snapshot, IReadOnlyCollection<ConfiguredSetupComponentSnapshot> configuredSnapshots)
    {
        var builder = ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds.ToBuilder();

        foreach (ConfiguredSetupComponentSnapshot configuredSnapshot in configuredSnapshots)
        {
            Assumes.False(configuredSnapshot.IsEmpty);

            foreach (string componentId in configuredSnapshot.ComponentIds)
            {
                builder.Add(componentId);
            }
        }

        ImmutableHashSet<string> componentIds = builder.ToImmutable();

        if (snapshot is not null && componentIds.SetEquals(snapshot.ComponentIds))
        {
            return false;
        }

        snapshot = new UnconfiguredSetupComponentSnapshot(componentIds);
        return true;
    }
}
