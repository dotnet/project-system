// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static readonly CategoryPropertiesAvailableStatus s_requestedCategoryProperties;
        private static readonly UIPropertyPropertiesAvailableStatus s_requestedPropertyProperties;

        protected readonly List<(IEntityValue projectEntity, EntityIdentity)> AddedLaunchProfiles = new();
        protected readonly List<(IEntityValue projectEntity, EntityIdentity)> RemovedLaunchProfiles = new();

        static LaunchProfileActionBase()
        {
            s_requestedLaunchProfileProperties = new LaunchProfilePropertiesAvailableStatus();
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.CategoriesPropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.CommandNamePropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.NamePropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.OrderPropertyName);
            s_requestedLaunchProfileProperties.RequireProperty(LaunchProfileType.PropertiesPropertyName);
            s_requestedLaunchProfileProperties.Freeze();

            s_requestedCategoryProperties = new CategoryPropertiesAvailableStatus();
            s_requestedCategoryProperties.RequireProperty(CategoryType.DisplayNamePropertyName);
            s_requestedCategoryProperties.RequireProperty(CategoryType.NamePropertyName);
            s_requestedCategoryProperties.RequireProperty(CategoryType.OrderPropertyName);
            s_requestedCategoryProperties.Freeze();

            s_requestedPropertyProperties = new UIPropertyPropertiesAvailableStatus();
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.CategoryNamePropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.ConfigurationIndependentPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.DescriptionPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.DependsOnPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.DisplayNamePropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.HelpUrlPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.IsReadOnlyPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.IsVisiblePropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.NamePropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.OrderPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.SearchTermsPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.TypePropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.VisibilityConditionPropertyName);
            s_requestedPropertyProperties.Freeze();
        }

        public async Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
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
                        if (await handler.RetrieveLaunchProfileEntityAsync(request.QueryExecutionContext, addedProfileId, s_requestedLaunchProfileProperties) is IEntityValue launchProfileEntity)
                        {
                            if (launchProfileEntity is IEntityValueFromProvider launchProfileEntityFromProvider
                                && launchProfileEntityFromProvider.ProviderState is ContextAndRuleProviderState state)
                            {
                                // This is a bit of a hack. We can safely assume that we should update the query
                                // with the entity for the new launch profile. However, there is no way for us to
                                // know which properties or child entities are desired. Here we make the somewhat
                                // arbitrary decision to include the categories and properties, but not the propery
                                // values. We already requested the non-entity properties when creating the entity
                                // for the launch profile.

                                ImmutableArray<IEntityValue> categories = ImmutableArray.CreateRange(
                                    CategoryDataProducer.CreateCategoryValues(request.QueryExecutionContext, launchProfileEntity, state.Rule, s_requestedCategoryProperties));
                                launchProfileEntity.SetRelatedEntities(LaunchProfileType.CategoriesPropertyName, categories);

                                ImmutableArray<IEntityValue> properties = ImmutableArray.CreateRange(
                                    UIPropertyDataProducer.CreateUIPropertyValues(request.QueryExecutionContext, launchProfileEntity, state.ProjectState, state.PropertiesContext, state.Rule, s_requestedPropertyProperties));
                                launchProfileEntity.SetRelatedEntities(LaunchProfileType.PropertiesPropertyName, properties);
                            }

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

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
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
