// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
/// Providers a wrapper around the what is considered the active debugging framework.
/// </summary>
[Export(typeof(IActiveDebugFrameworkServices))]
[AppliesTo(ProjectCapability.DotNet)]
internal class ActiveDebugFrameworkServices : IActiveDebugFrameworkServices
{
    private readonly IActiveConfiguredProjectsProvider _activeConfiguredProjectsProvider;
    private readonly IUnconfiguredProjectCommonServices _commonProjectServices;

    [ImportingConstructor]
    public ActiveDebugFrameworkServices(IActiveConfiguredProjectsProvider activeConfiguredProjectsProvider, IUnconfiguredProjectCommonServices commonProjectServices)
    {
        _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;
        _commonProjectServices = commonProjectServices;
    }

    public async Task<List<string>?> GetProjectFrameworksAsync()
    {
        // It is important that we return the frameworks in the order they are specified in the project to ensure the default is set
        // correctly. 
        ConfigurationGeneral props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

        string? targetFrameworks = (string?)await props.TargetFrameworks.GetValueAsync();

        if (Strings.IsNullOrWhiteSpace(targetFrameworks))
        {
            return null;
        }

        return BuildUtilities.GetPropertyValues(targetFrameworks).ToList();
    }

    public async Task SetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
    {
        ProjectDebugger props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync();

        await props.ActiveDebugFramework.SetValueAsync(activeFramework);
    }

    public async Task<string?> GetActiveDebuggingFrameworkPropertyAsync()
    {
        ProjectDebugger props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync();

        return await props.ActiveDebugFramework.GetValueAsync() as string;
    }

    public async Task<ConfiguredProject?> GetConfiguredProjectForActiveFrameworkAsync()
    {
        ActiveConfiguredObjects<ConfiguredProject>? projects = await _activeConfiguredProjectsProvider.GetActiveConfiguredProjectsAsync();

        if (projects is null || projects.Objects.IsEmpty)
        {
            return null;
        }

        if (projects.Objects.Length is 1)
        {
            return projects.Objects[0];
        }

        bool isMultiTargeting = projects.Objects.All(project => project.ProjectConfiguration.IsCrossTargeting());

        if (isMultiTargeting)
        {
            string? activeDebugFramework = await GetActiveDebuggingFrameworkPropertyAsync();

            ConfiguredProject? project = FindProject(activeDebugFramework);

            if (project is null)
            {
                // The expected debug framework was not found, so we need to pick one.
                // Use the first target as defined in the TargetFrameworks property. This is treated specially.
                // We must pick the first that was defined can't just select the first one. If activeFramework is not set we must pick the first one as defined by the 
                // targetFrameworks property. So we need the order as returned by GetProjectFrameworks()
                if (await GetProjectFrameworksAsync() is [string firstFramework, ..])
                {
                    project = FindProject(firstFramework);
                }
            }

            System.Diagnostics.Debug.Assert(project is not null, "Unable to determine debug project configuration.");

            return project;
        }
        else
        {
            System.Diagnostics.Debug.Assert(projects.Objects.Length == 1, "Expected only one active configured project when not cross-targeting.");

            return projects.Objects[0];
        }

        ConfiguredProject? FindProject(string? targetFramework)
        {
            if (Strings.IsNullOrWhiteSpace(targetFramework))
            {
                return null;
            }

            foreach (ConfiguredProject project in projects.Objects)
            {
                string tf = project.ProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];

                if (StringComparers.ConfigurationDimensionValues.Equals(tf, targetFramework))
                {
                    return project;
                }
            }

            return null;
        }
    }
}
