// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Base class for <see cref="IQueryActionExecutor"/>s that modify launch settings and launch profiles.
    /// </summary>
    internal abstract class LaunchProfileActionBase : QueryDataProducerBase<IEntityValue>, IQueryActionExecutor
    {
        protected readonly List<(IEntityValue projectEntity, ILaunchProfile profile)> AddedLaunchProfiles = new();
        protected readonly List<(IEntityValue projectEntity, string profileName)> RemovedLaunchProfiles = new();

        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            foreach (IGrouping<IEntityValue, (IEntityValue projectEntity, ILaunchProfile profile)> group in AddedLaunchProfiles.GroupBy(item => item.projectEntity))
            {
                var projectValue = (IEntityValueFromProvider)group.Key;

                var returnedLaunchProfiles = new List<IEntityValue>();
                if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
                {
                    returnedLaunchProfiles.AddRange(exitingProfiles);
                }

                foreach ((IEntityValue projectEntity, ILaunchProfile profile) in group)
                {
                    Assumes.NotNull(profile.Name);
                    var newProfileEntity = new LaunchProfileValue(
                        projectValue.EntityRuntime,
                        LaunchProfileDataProducer.CreateLaunchProfileId(projectValue, LaunchProfileProjectItemProvider.ItemType, profile.Name),
                        new LaunchProfilePropertiesAvailableStatus())
                    {
                        CommandName = profile.CommandName,
                        Name = profile.Name
                    };

                    returnedLaunchProfiles.Add(newProfileEntity);
                }

                projectValue.SetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, returnedLaunchProfiles);
            }

            foreach (IGrouping<IEntityValue, (IEntityValue projectEntity, string profileName)> group in RemovedLaunchProfiles.GroupBy(item => item.projectEntity))
            {
                var projectValue = (IEntityValueFromProvider)group.Key;

                var returnedLaunchProfiles = new List<IEntityValue>();
                if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
                {
                    returnedLaunchProfiles.AddRange(exitingProfiles);
                }

                foreach ((IEntityValue projectEntity, string profileName) in group)
                {
                    var removedProfileId = LaunchProfileDataProducer.CreateLaunchProfileId(projectValue, LaunchProfileProjectItemProvider.ItemType, profileName);
                    IEntityValue? entityToRemove = returnedLaunchProfiles.FirstOrDefault(entity => ((IEntityWithId)entity).Id.Equals(removedProfileId));
                    if (entityToRemove is not null)
                    {
                        returnedLaunchProfiles.Remove(entityToRemove);
                    }
                }

                projectValue.SetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, returnedLaunchProfiles);
            }

            return ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        public async Task ReceiveResultAsync(QueryProcessResult<IEntityValue> result)
        {
            result.Request.QueryExecutionContext.CancellationToken.ThrowIfCancellationRequested();

            if (((IEntityValueFromProvider)result.Result).ProviderState is UnconfiguredProject project
                && project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsActionService>() is ILaunchSettingsActionService launchSettingsActionService
                && project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider
                && project.Services.ExportProvider.GetExportedValueOrDefault<LaunchSettingsTracker>() is LaunchSettingsTracker tracker)
            {
                await ExecuteAsync(result.Result, launchSettingsActionService, result.Request.QueryExecutionContext.CancellationToken);

                if (launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
                {
                    result.Request.QueryExecutionContext.ReportUpdatedDataVersion(tracker.VersionKey, versionedLaunchSettings.Version);
                }
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }

        protected abstract Task ExecuteAsync(IEntityValue projectEntity, ILaunchSettingsActionService launchSettingsProvider, CancellationToken cancellationToken);
    }
}
