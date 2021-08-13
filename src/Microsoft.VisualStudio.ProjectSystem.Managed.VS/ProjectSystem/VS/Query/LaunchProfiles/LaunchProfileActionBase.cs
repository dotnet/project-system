// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Base class for <see cref="IQueryActionExecutor"/>s that modify launch settings and launch profiles.
    /// </summary>
    internal abstract class LaunchProfileActionBase : QueryDataProducerBase<IEntityValue>, IQueryActionExecutor
    {
        /// <summary>
        /// The set of entity properties to request when we create a new launch profile and
        /// need to obtain an entity for it.
        /// </summary>
        private static readonly LaunchProfilePropertiesAvailableStatus s_requestedLaunchProfileProperties;

        protected readonly List<(IEntityValue projectEntity, EntityIdentity)> AddedLaunchProfiles = new();
        protected readonly List<(IEntityValue projectEntity, EntityIdentity)> RemovedLaunchProfiles = new();

        static LaunchProfileActionBase()
        {
            // TODO: Note that even though we indicate here that the categories and properties
            // child entities should be included we are still responsible for populating them
            // manually.
            s_requestedLaunchProfileProperties = new LaunchProfilePropertiesAvailableStatus();
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.CategoriesPropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.CommandNamePropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.NamePropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.OrderPropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.PropertiesPropertyName);
            s_requestedLaunchProfileProperties.Freeze();
        }

        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            foreach (IGrouping<IEntityValue, (IEntityValue projectEntity, EntityIdentity profileId)> group in AddedLaunchProfiles.GroupBy(item => item.projectEntity))
            {
                var projectValue = (IEntityValueFromProvider)group.Key;
                if (projectValue.ProviderState is UnconfiguredProject project
                    && project.Services.ExportProvider.GetExportedValueOrDefault<IProjectLaunchProfileHandler>() is IProjectLaunchProfileHandler handler)
                {
                    var returnedLaunchProfiles = new List<IEntityValue>();
                    if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
                    {
                        returnedLaunchProfiles.AddRange(exitingProfiles);
                    }

                    foreach ((IEntityValue projectEntity, EntityIdentity addedProfileId) in group)
                    {
                        if (handler.RetrieveLaunchProfileEntityAsync(request.QueryExecutionContext, addedProfileId, s_requestedLaunchProfileProperties) is IEntityValue launchProfileEntity)
                        {
                            // TODO: Actually populate the categories and properties child entities.
                            returnedLaunchProfiles.Add(launchProfileEntity);
                        }
                    }

                    projectValue.SetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, returnedLaunchProfiles);
                }
            }

            foreach (IGrouping<IEntityValue, (IEntityValue projectEntity, EntityIdentity profileId)> group in RemovedLaunchProfiles.GroupBy(item => item.projectEntity))
            {
                var projectValue = (IEntityValueFromProvider)group.Key;

                var returnedLaunchProfiles = new List<IEntityValue>();
                if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
                {
                    returnedLaunchProfiles.AddRange(exitingProfiles);
                }

                foreach ((IEntityValue projectEntity, EntityIdentity removedProfileId) in group)
                {
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
                && project.Services.ExportProvider.GetExportedValueOrDefault<IProjectLaunchProfileHandler>() is IProjectLaunchProfileHandler launchSettingsActionService)
            {
                await ExecuteAsync(result.Request.QueryExecutionContext, result.Result, launchSettingsActionService, result.Request.QueryExecutionContext.CancellationToken);
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }

        protected abstract Task ExecuteAsync(IQueryExecutionContext queryExecutionContext, IEntityValue projectEntity, IProjectLaunchProfileHandler launchProfileHandler, CancellationToken cancellationToken);
    }
}
