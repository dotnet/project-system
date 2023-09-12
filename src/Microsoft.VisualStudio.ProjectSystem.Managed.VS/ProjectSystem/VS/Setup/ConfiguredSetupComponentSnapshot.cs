// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Immutable snapshot of the components required by a configured project.
/// </summary>
internal sealed class ConfiguredSetupComponentSnapshot
{
    public static ConfiguredSetupComponentSnapshot Empty { get; } = new();

    private readonly bool _requiresWebComponent;
    private readonly string? _netCoreTargetFrameworkVersion;
    private readonly ImmutableHashSet<string> _suggestedWorkloadComponentIds;

    public ImmutableHashSet<string> ComponentIds { get; }
    
    public bool IsEmpty { get; }

    private ConfiguredSetupComponentSnapshot()
    {
        _suggestedWorkloadComponentIds = ComponentIds = ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds;
        IsEmpty = true;
    }
    
    private ConfiguredSetupComponentSnapshot(bool requiresWebComponent, string? netCoreTargetFrameworkVersion, ImmutableHashSet<string> suggestedWorkloadComponentIds)
    {
        _requiresWebComponent = requiresWebComponent;
        _netCoreTargetFrameworkVersion = netCoreTargetFrameworkVersion;
        _suggestedWorkloadComponentIds = suggestedWorkloadComponentIds;

        ImmutableHashSet<string> componentIds = suggestedWorkloadComponentIds;

        if (requiresWebComponent)
        {
            componentIds = componentIds.Add(SetupComponentReferenceData.WebComponentId);
        }

        if (netCoreTargetFrameworkVersion is not null && SetupComponentReferenceData.TryGetComponentIdByNetCoreTargetFrameworkVersion(netCoreTargetFrameworkVersion, out string? runtimeComponentId))
        {
            componentIds = componentIds.Add(runtimeComponentId);
        }

        ComponentIds = componentIds;
    }

    public ConfiguredSetupComponentSnapshot Update(
        IProjectSubscriptionUpdate evaluationUpdate,
        IProjectSubscriptionUpdate buildUpdate,
        IProjectCapabilitiesSnapshot capabilities)
    {
        // We use bitwise | here instead of logical || to prevent short circuiting.
        if (IsEmpty |
            ProcessCapabilities(out bool requiresWebComponent) |
            ProcessEvaluationUpdate(out string? netCoreTargetFrameworkVersion) |
            ProcessBuildUpdate(out ImmutableHashSet<string> suggestedWorkloadComponentIds))
        {
            return new(requiresWebComponent, netCoreTargetFrameworkVersion, suggestedWorkloadComponentIds);
        }

        return this;

        bool ProcessCapabilities(out bool requiresWebComponent)
        {
            requiresWebComponent = RequiresWebComponent();
            return requiresWebComponent != _requiresWebComponent;

            bool RequiresWebComponent()
            {
                // Handle scenarios where Visual Studio developer may have an install of VS with only the desktop workload
                // and a developer may open a WPF/WinForms project (or edit an existing one) to be able to create a hybrid app (WPF + Blazor web).

                // DotNetCoreRazor && (WindowsForms || WPF)
                return capabilities.IsProjectCapabilityPresent(ProjectCapability.DotNetCoreRazor)
                    && (capabilities.IsProjectCapabilityPresent(ProjectCapability.WindowsForms) || capabilities.IsProjectCapabilityPresent(ProjectCapability.WPF));
            }
        }

        bool ProcessEvaluationUpdate(out string? netCoreTargetFrameworkVersion)
        {
            IProjectChangeDescription change = evaluationUpdate.ProjectChanges[ConfigurationGeneral.SchemaName];

            if (change.Difference.ChangedProperties.Count == 0)
            {
                netCoreTargetFrameworkVersion = _netCoreTargetFrameworkVersion;
                return false;
            }

            IImmutableDictionary<string, string> properties = change.After.Properties;

            string? targetFrameworkIdentifier = properties.GetStringProperty(ConfigurationGeneral.TargetFrameworkIdentifierProperty);

            netCoreTargetFrameworkVersion = StringComparers.FrameworkIdentifiers.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetCoreApp)
                ? properties.GetStringProperty(ConfigurationGeneral.TargetFrameworkVersionProperty)
                : null;
            return netCoreTargetFrameworkVersion != _netCoreTargetFrameworkVersion;
        }

        bool ProcessBuildUpdate(out ImmutableHashSet<string> suggestedWorkloadComponentIds)
        {
            IProjectChangeDescription change = buildUpdate.ProjectChanges[SuggestedWorkload.SchemaName];

            if (!change.Difference.AnyChanges)
            {
                suggestedWorkloadComponentIds = _suggestedWorkloadComponentIds;
                return false;
            }

            IImmutableDictionary<string, IImmutableDictionary<string, string>> suggestedWorkloads = change.After.Items;

            if (suggestedWorkloads.Count == 0)
            {
                suggestedWorkloadComponentIds = ImmutableHashSet<string>.Empty;
                return false;
            }

            ImmutableHashSet<string>.Builder? componentIds = null;

            foreach ((string workloadName, IImmutableDictionary<string, string> metadata) in suggestedWorkloads)
            {
                if (metadata.GetStringProperty(SuggestedWorkload.VisualStudioComponentIdsProperty) is string ids)
                {
                    componentIds ??= ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds.ToBuilder();
                    componentIds.AddRange(new LazyStringSplit(ids, ';').Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()));
                }
                else if (metadata.GetStringProperty(SuggestedWorkload.VisualStudioComponentIdProperty) is string id)
                {
                    componentIds ??= ImmutableStringHashSet.EmptyVisualStudioSetupComponentIds.ToBuilder();
                    componentIds.Add(id.Trim());
                }
            }

            suggestedWorkloadComponentIds = componentIds?.ToImmutable() ?? ImmutableHashSet<string>.Empty;
            return true;
        }
    }
}
