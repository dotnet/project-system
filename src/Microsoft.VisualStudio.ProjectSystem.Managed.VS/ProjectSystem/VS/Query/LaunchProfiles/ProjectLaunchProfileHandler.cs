// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    [Export(typeof(IProjectLaunchProfileHandler))]
    internal class ProjectLaunchProfileHandler : IProjectLaunchProfileHandler
    {
        private static readonly string s_commandNameMetadataName = "CommandName";
        private static readonly string s_launchProfileSourceItemTypeValue = "LaunchProfile";

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly LaunchSettingsTracker _launchSettingsTracker;

        [ImportingConstructor]
        public ProjectLaunchProfileHandler(
            UnconfiguredProject project,
            ILaunchSettingsProvider launchSettingsProvider,
            LaunchSettingsTracker launchSettingsTracker)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _launchSettingsTracker = launchSettingsTracker;
        }

        public async Task<IEntityValue?> RetrieveLaunchProfileEntityAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id, ILaunchProfilePropertiesAvailableStatus requestedProperties)
        {
            string desiredProfileName = ValidateIdAndExtractProfileName(id);

            if (await _project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot();

                if (launchSettings is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    queryExecutionContext.ReportInputDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);

                    IProjectState projectState = new LaunchProfileProjectState(_project, _launchSettingsProvider, _launchSettingsTracker);

                    foreach ((int index, ProjectSystem.Debug.ILaunchProfile profile) in launchSettings.Profiles.WithIndices())
                    {
                        if (StringComparers.LaunchProfileNames.Equals(profile.Name, desiredProfileName)
                            && !Strings.IsNullOrEmpty(profile.CommandName))
                        {
                            foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                            {
                                if (rule.Metadata.TryGetValue(s_commandNameMetadataName, out object? commandNameObj)
                                    && commandNameObj is string commandName
                                    && StringComparers.LaunchProfileCommandNames.Equals(commandName, profile.CommandName))
                                {
                                    QueryProjectPropertiesContext propertiesContext = new(
                                        isProjectFile: true,
                                        file: _project.FullPath,
                                        itemType: LaunchProfileProjectItemProvider.ItemType,
                                        itemName: profile.Name);

                                    IEntityValue launchProfileValue = CreateLaunchProfileValue(
                                        queryExecutionContext,
                                        id,
                                        propertiesContext,
                                        rule,
                                        index,
                                        projectState,
                                        requestedProperties);
                                    return launchProfileValue;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<IEnumerable<IEntityValue>> RetrieveAllLaunchProfileEntitiesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ILaunchProfilePropertiesAvailableStatus requestedProperties)
        {
            if (await _project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot();
                return createLaunchProfileValues(launchSettings);
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createLaunchProfileValues(ILaunchSettings launchSettings)
            {
                Dictionary<string, Rule> debugRules = new();
                foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                {
                    if (rule.Metadata.TryGetValue(s_commandNameMetadataName, out object? commandNameObj)
                        && commandNameObj is string commandName)
                    {
                        debugRules[commandName] = rule;
                    }
                }

                if (launchSettings is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    queryExecutionContext.ReportInputDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
                }

                IProjectState projectState = new LaunchProfileProjectState(_project, _launchSettingsProvider, _launchSettingsTracker);

                foreach ((int index, ProjectSystem.Debug.ILaunchProfile profile) in launchSettings.Profiles.WithIndices())
                {
                    if (!Strings.IsNullOrEmpty(profile.Name)
                        && !Strings.IsNullOrEmpty(profile.CommandName)
                        && debugRules.TryGetValue(profile.CommandName, out Rule rule))
                    {
                        QueryProjectPropertiesContext propertiesContext = new(
                            isProjectFile: true,
                            file: _project.FullPath,
                            itemType: LaunchProfileProjectItemProvider.ItemType,
                            itemName: profile.Name);

                        EntityIdentity id = CreateLaunchProfileId(parent, profile.Name);
                        IEntityValue launchProfileValue = CreateLaunchProfileValue(queryExecutionContext, id, propertiesContext, rule, index, projectState, requestedProperties);
                        yield return launchProfileValue;
                    }
                }
            }
        }

        public async Task<EntityIdentity?> AddLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string commandName, string? newProfileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            newProfileName ??= await GetNewProfileNameAsync(cancellationToken);

            await _launchSettingsProvider.AddOrUpdateProfileAsync(
                new WritableLaunchProfile
                {
                    Name = newProfileName,
                    CommandName = commandName
                }.ToLaunchProfile(),
                addToFront: false);

            if (_launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
            {
                queryExecutionContext.ReportUpdatedDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
            }

            return CreateLaunchProfileId(parent, newProfileName);
        }

        public async Task<EntityIdentity?> DuplicateLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string currentProfileName, string? newProfileName, string? newProfileCommandName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(cancellationToken);

            ProjectSystem.Debug.ILaunchProfile? existingProfile = launchSettings.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, currentProfileName));
            if (existingProfile is not null)
            {
                newProfileName ??= await GetNewProfileNameAsync(cancellationToken);
                newProfileCommandName ??= existingProfile.CommandName;

                var writableProfile = new WritableLaunchProfile(existingProfile)
                {
                    Name = newProfileName,
                    CommandName = newProfileCommandName
                };

                await _launchSettingsProvider.AddOrUpdateProfileAsync(writableProfile.ToLaunchProfile(), addToFront: false);

                if (_launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    queryExecutionContext.ReportUpdatedDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
                }

                return CreateLaunchProfileId(parent, newProfileName);
            }

            return null;
        }

        public async Task<EntityIdentity?> RemoveLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string profileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _launchSettingsProvider.RemoveProfileAsync(profileName);

            if (_launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
            {
                queryExecutionContext.ReportUpdatedDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
            }

            return CreateLaunchProfileId(parent, profileName);
        }

        public async Task<(EntityIdentity oldProfileId, EntityIdentity newProfileId)?> RenameLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string currentProfileName, string newProfileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(cancellationToken);

            ProjectSystem.Debug.ILaunchProfile? existingProfile = launchSettings.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, currentProfileName));
            if (existingProfile is not null)
            {
                var writableProfile = new WritableLaunchProfile(existingProfile)
                {
                    Name = newProfileName
                };

                await _launchSettingsProvider.RemoveProfileAsync(currentProfileName);
                await _launchSettingsProvider.AddOrUpdateProfileAsync(writableProfile.ToLaunchProfile(), addToFront: false);

                if (_launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    queryExecutionContext.ReportUpdatedDataVersion(_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version);
                }

                return (CreateLaunchProfileId(parent, currentProfileName), CreateLaunchProfileId(parent, newProfileName));
            }

            return null;
        }

        private static IEntityValue CreateLaunchProfileValue(IQueryExecutionContext queryExecutionContext, EntityIdentity id, QueryProjectPropertiesContext propertiesContext, Rule rule, int order, IProjectState cache, ILaunchProfilePropertiesAvailableStatus properties)
        {
            LaunchProfileSnapshot newLaunchProfile = new(queryExecutionContext.EntityRuntime, id, new LaunchProfilePropertiesAvailableStatus());

            if (properties.Name)
            {
                newLaunchProfile.Name = propertiesContext.ItemName;
            }

            if (properties.DisplayName)
            {
                newLaunchProfile.DisplayName = propertiesContext.ItemName;
            }

            if (properties.CommandName)
            {
                if (rule.Metadata.TryGetValue(s_commandNameMetadataName, out object? commandNameObj)
                    && commandNameObj is string commandName)
                {
                    newLaunchProfile.CommandName = commandName;
                }
            }

            if (properties.Order)
            {
                newLaunchProfile.Order = order;
            }

            ((IEntityValueFromProvider)newLaunchProfile).ProviderState = new ContextAndRuleProviderState(cache, propertiesContext, rule);

            return newLaunchProfile;
        }

        /// <summary>
        /// Creates an <see cref="EntityIdentity"/> representing a launch profile with the
        /// name <paramref name="profileName"/>.
        /// </summary>
        private static EntityIdentity CreateLaunchProfileId(IEntityValue parent, string profileName)
        {
            return new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new Dictionary<string, string>
                {
                    { ProjectModelIdentityKeys.SourceItemType, s_launchProfileSourceItemTypeValue },
                    { ProjectModelIdentityKeys.SourceItemName, profileName }
                });
        }

        /// <summary>
        /// Validates that <paramref name="id"/> represents a launch profile (or a child
        /// entity of a launch profile) and throws if it does not. Returns the profile name.
        /// </summary>
        /// <remarks>
        /// We expect that the Project Query engine will only give us entities and entity
        /// IDs that we know how to handle, as specified by the metadata on our
        /// implementations of <see cref="IQueryByIdDataProvider"/> and <see cref="IQueryByRelationshipDataProvider"/>.
        /// Anything else warrants an exception.
        /// </remarks>
        private static string ValidateIdAndExtractProfileName(EntityIdentity id)
        {
            Assumes.True(id.TryGetValue(ProjectModelIdentityKeys.SourceItemType, out string? type));
            Assumes.True(StringComparers.ItemTypes.Equals(type, s_launchProfileSourceItemTypeValue));
            Assumes.True(id.TryGetValue(ProjectModelIdentityKeys.SourceItemName, out string? name));

            return name;
        }

        private async Task<string> GetNewProfileNameAsync(CancellationToken cancellationToken = default)
        {
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(cancellationToken);

            string? newProfileName = null;
            for (int i = 1; newProfileName is null; i++)
            {
                string potentialProfileName = string.Format(VSResources.DefaultNewProfileName, i);
                if (!launchSettings.Profiles.Any(profile => StringComparers.LaunchProfileNames.Equals(potentialProfileName, profile.Name)))
                {
                    newProfileName = potentialProfileName;
                }
            }

            return newProfileName;
        }
    }
}
