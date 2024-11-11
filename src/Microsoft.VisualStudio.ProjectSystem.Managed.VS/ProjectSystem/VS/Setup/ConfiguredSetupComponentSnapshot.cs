// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Immutable snapshot of the VS setup components required by a configured project.
/// </summary>
internal sealed class ConfiguredSetupComponentSnapshot
{
    public static ConfiguredSetupComponentSnapshot Empty { get; } = new(requiresWebComponent: false, componentIds: ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds);

    private readonly bool _requiresWebComponent;

    public ImmutableHashSet<string> ComponentIds { get; }

    private ConfiguredSetupComponentSnapshot(bool requiresWebComponent, ImmutableHashSet<string> componentIds)
    {
        _requiresWebComponent = requiresWebComponent;
        ComponentIds = componentIds;
    }

    /// <summary>
    /// Applies changes to the current (immutable) snapshot, producing a new snapshot.
    /// </summary>
    /// <returns>The updated snapshot, or the same instance if no changes were required.</returns>
    public ConfiguredSetupComponentSnapshot Update(IProjectSubscriptionUpdate buildUpdate, IProjectCapabilitiesSnapshot capabilities)
    {
        ImmutableHashSet<string> componentIds;

        bool requiresWebComponent;

        ProcessCapabilities();
        ProcessBuildUpdate();

        if (ReferenceEquals(componentIds, ComponentIds))
        {
            return this;
        }

        return new(requiresWebComponent, componentIds);

        void ProcessCapabilities()
        {
            const string webComponentId = "Microsoft.VisualStudio.Component.Web";

            requiresWebComponent = RequiresWebComponent();

            componentIds = (requiresWebComponent, _requiresWebComponent) switch
            {
                (true, false) => ComponentIds.Add(webComponentId),
                (false, true) => ComponentIds.Remove(webComponentId),
                _ => ComponentIds
            };

            bool RequiresWebComponent()
            {
                // Handle scenarios where Visual Studio developer may have an install of VS with only the desktop workload
                // and a developer may open a WPF/WinForms project (or edit an existing one) to be able to create a hybrid app (WPF + Blazor web).

                // DotNetCoreRazor && (WindowsForms || WPF)
                return capabilities.IsProjectCapabilityPresent(ProjectCapability.DotNetCoreRazor)
                    && (capabilities.IsProjectCapabilityPresent(ProjectCapability.WindowsForms) || capabilities.IsProjectCapabilityPresent(ProjectCapability.WPF));
            }
        }

        void ProcessBuildUpdate()
        {
            IProjectChangeDescription change = buildUpdate.ProjectChanges[SuggestedVisualStudioComponentId.SchemaName];

            if (!change.Difference.AnyChanges)
            {
                return;
            }

            var builder = ComponentIds.ToBuilder();

            foreach (string addedItem in change.Difference.AddedItems)
            {
                builder.Add(addedItem);
            }

            foreach (string removedItem in change.Difference.RemovedItems)
            {
                builder.Remove(removedItem);
            }

            foreach ((string before, string after) in change.Difference.RenamedItems)
            {
                builder.Remove(before);
                builder.Add(after);
            }

            componentIds = builder.ToImmutable();
        }
    }
}
