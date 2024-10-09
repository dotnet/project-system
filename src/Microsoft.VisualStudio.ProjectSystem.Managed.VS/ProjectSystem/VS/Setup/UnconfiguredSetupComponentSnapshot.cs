// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Immutable snapshot of the components required by an unconfigured project.
/// </summary>
internal sealed record class UnconfiguredSetupComponentSnapshot(ImmutableHashSet<string> ComponentIds)
{
    public static UnconfiguredSetupComponentSnapshot Empty { get; } = new(ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds);

    public static bool TryUpdate(ref UnconfiguredSetupComponentSnapshot snapshot, IReadOnlyCollection<ConfiguredSetupComponentSnapshot> configuredSnapshots)
    {
        ImmutableHashSet<string>.Builder? builder = null;

        foreach (ConfiguredSetupComponentSnapshot configuredSnapshot in configuredSnapshots)
        {
            foreach (string componentId in configuredSnapshot.ComponentIds)
            {
                builder ??= ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds.ToBuilder();
                builder.Add(componentId);
            }
        }

        if (builder is null)
        {
            bool wasEmpty = ReferenceEquals(snapshot, Empty);
            snapshot = Empty;
            return !wasEmpty;
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
