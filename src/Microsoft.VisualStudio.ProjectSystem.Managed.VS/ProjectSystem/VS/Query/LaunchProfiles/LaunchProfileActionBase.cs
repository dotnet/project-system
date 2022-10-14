// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;

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
        private static readonly UIPropertyEditorPropertiesAvailableStatus s_requestedEditorProperties;
        private static readonly UIEditorMetadataPropertiesAvailableStatus s_requestedEditorMetadataProperties;
        private static readonly UIPropertyValuePropertiesAvailableStatus s_requestedValueProperties;
        private static readonly ConfigurationDimensionPropertiesAvailableStatus s_requestedConfigurationDimensionProperties;
        private static readonly SupportedValuePropertiesAvailableStatus s_requestedSupportedValueProperties;

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
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.DimensionVisibilityConditionPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.ConfiguredValueVisibilityConditionPropertyName);
            s_requestedPropertyProperties.RequireProperty(UIPropertyType.IsReadOnlyConditionPropertyName);
            s_requestedPropertyProperties.Freeze();

            s_requestedEditorProperties = new UIPropertyEditorPropertiesAvailableStatus();
            s_requestedEditorProperties.RequireProperty(UIPropertyEditorType.MetadataPropertyName);
            s_requestedEditorProperties.RequireProperty(UIPropertyEditorType.NamePropertyName);
            s_requestedEditorProperties.Freeze();

            s_requestedEditorMetadataProperties = new UIEditorMetadataPropertiesAvailableStatus();
            s_requestedEditorMetadataProperties.RequireProperty(UIEditorMetadataType.NamePropertyName);
            s_requestedEditorMetadataProperties.RequireProperty(UIEditorMetadataType.ValuePropertyName);
            s_requestedEditorMetadataProperties.Freeze();

            s_requestedValueProperties = new UIPropertyValuePropertiesAvailableStatus();
            s_requestedValueProperties.RequireProperty(UIPropertyValueType.ConfigurationDimensionsPropertyName);
            s_requestedValueProperties.RequireProperty(UIPropertyValueType.EvaluatedValuePropertyName);
            s_requestedValueProperties.RequireProperty(UIPropertyValueType.SupportedValuesPropertyName);
            s_requestedValueProperties.RequireProperty(UIPropertyValueType.UnevaluatedValuePropertyName);
            s_requestedValueProperties.RequireProperty(UIPropertyValueType.ValueDefinedInContextPropertyName);
            s_requestedValueProperties.Freeze();

            s_requestedSupportedValueProperties = new SupportedValuePropertiesAvailableStatus();
            s_requestedSupportedValueProperties.RequireProperty(SupportedValueType.DisplayNamePropertyName);
            s_requestedSupportedValueProperties.RequireProperty(SupportedValueType.ValuePropertyName);
            s_requestedSupportedValueProperties.Freeze();

            s_requestedConfigurationDimensionProperties = new ConfigurationDimensionPropertiesAvailableStatus();
            s_requestedConfigurationDimensionProperties.RequireProperty(ConfigurationDimensionType.NamePropertyName);
            s_requestedConfigurationDimensionProperties.RequireProperty(ConfigurationDimensionType.ValuePropertyName);
            s_requestedConfigurationDimensionProperties.Freeze();
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
                    if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
                    {
                        returnedLaunchProfiles.AddRange(exitingProfiles);
                    }

                    foreach ((IEntityValue projectEntity, EntityIdentity addedProfileId) in group)
                    {
                        if (await handler.RetrieveLaunchProfileEntityAsync(request.QueryExecutionContext, addedProfileId, s_requestedLaunchProfileProperties) is IEntityValue launchProfileEntity)
                        {
                            if (launchProfileEntity is IEntityValueFromProvider { ProviderState: ContextAndRuleProviderState state })
                            {
                                // This is a bit of a hack. We can safely assume that we should update the query
                                // with the entity for the new launch profile. However, there is no way for us to
                                // know which properties or child entities are desired. Here we make the somewhat
                                // arbitrary decision to include the categories and properties, but not the property
                                // values. We already requested the non-entity properties when creating the entity
                                // for the launch profile.

                                // Add categories to the profile
                                ImmutableArray<IEntityValue> categories = ImmutableArray.CreateRange(
                                    CategoryDataProducer.CreateCategoryValues(request.QueryExecutionContext, launchProfileEntity, state.Rule, s_requestedCategoryProperties));
                                launchProfileEntity.SetRelatedEntities(LaunchProfileType.CategoriesPropertyName, categories);

                                // Add properties to the profile
                                ImmutableArray<IEntityValue> properties = ImmutableArray.CreateRange(
                                    UIPropertyDataProducer.CreateUIPropertyValues(request.QueryExecutionContext, launchProfileEntity, state.ProjectState, state.PropertiesContext, state.Rule, s_requestedPropertyProperties));
                                launchProfileEntity.SetRelatedEntities(LaunchProfileType.PropertiesPropertyName, properties);
                                
                                await PopulateEditorsAndValues(properties);
                            }

                            returnedLaunchProfiles.Add(launchProfileEntity);
                        }
                    }

                    projectValue.SetRelatedEntities(ProjectSystem.Query.Metadata.ProjectType.LaunchProfilesPropertyName, returnedLaunchProfiles);
                }
            }

            foreach (IGrouping<IEntityValue, (IEntityValue projectEntity, EntityIdentity profileId)> group in RemovedLaunchProfiles.GroupBy(item => item.projectEntity))
            {
                var projectValue = (IEntityValueFromProvider)group.Key;

                var returnedLaunchProfiles = new List<IEntityValue>();
                if (projectValue.TryGetRelatedEntities(ProjectSystem.Query.Metadata.ProjectType.LaunchProfilesPropertyName, out IEnumerable<IEntityValue>? exitingProfiles))
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

                projectValue.SetRelatedEntities(ProjectSystem.Query.Metadata.ProjectType.LaunchProfilesPropertyName, returnedLaunchProfiles);
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);

            static async Task PopulateSupportedValuesAndConfigurations(ImmutableArray<IEntityValue> valueEntities)
            {
                foreach (IEntityValueFromProvider valueEntity in valueEntities)
                {
                    if (valueEntity.ProviderState is PropertyValueProviderState valueState)
                    {
                        // Add supported values to values
                        ImmutableArray<IEntityValue> supportedValues = ImmutableArray.CreateRange(
                            await SupportedValueDataProducer.CreateSupportedValuesAsync(valueEntity, valueState.Property, s_requestedSupportedValueProperties));
                        valueEntity.SetRelatedEntities(UIPropertyValueType.SupportedValuesPropertyName, supportedValues);

                        ImmutableArray<IEntityValue> configurationDimensions = ImmutableArray.CreateRange(
                            ConfigurationDimensionDataProducer.CreateProjectConfigurationDimensions(valueEntity, valueState.ProjectConfiguration, valueState.Property, s_requestedConfigurationDimensionProperties));
                        valueEntity.SetRelatedEntities(UIPropertyValueType.ConfigurationDimensionsPropertyName, configurationDimensions);
                    }
                }
            }

            static void PopulateEditorMetadata(ImmutableArray<IEntityValue> editors)
            {
                foreach (IEntityValueFromProvider editorEntity in editors)
                {
                    if (editorEntity.ProviderState is ValueEditor editorState)
                    {
                        // Add editor metadata to the editor
                        ImmutableArray<IEntityValue> editorMetadata = ImmutableArray.CreateRange(
                            UIEditorMetadataProducer.CreateMetadataValues(editorEntity, editorState, s_requestedEditorMetadataProperties));
                        editorEntity.SetRelatedEntities(UIPropertyEditorType.MetadataPropertyName, editorMetadata);
                    }
                }
            }

            async Task PopulateEditorsAndValues(ImmutableArray<IEntityValue> properties)
            {
                foreach (IEntityValueFromProvider propertyEntity in properties)
                {
                    if (propertyEntity.ProviderState is PropertyProviderState propertyProviderState)
                    {
                        // Add editors to the property
                        ImmutableArray<IEntityValue> editors = ImmutableArray.CreateRange(
                            UIPropertyEditorDataProducer.CreateEditorValues(request.QueryExecutionContext, propertyEntity, propertyProviderState.ContainingRule, propertyProviderState.PropertyName, s_requestedEditorProperties));
                        propertyEntity.SetRelatedEntities(UIPropertyType.EditorsPropertyName, editors);

                        PopulateEditorMetadata(editors);

                        // Add values to the property
                        ImmutableArray<IEntityValue> values = ImmutableArray.CreateRange(
                            await UIPropertyValueDataProducer.CreateUIPropertyValueValuesAsync(request.QueryExecutionContext, propertyEntity, propertyProviderState.ProjectState, propertyProviderState.ContainingRule, propertyProviderState.PropertiesContext, propertyProviderState.PropertyName, s_requestedValueProperties));
                        propertyEntity.SetRelatedEntities(UIPropertyType.ValuesPropertyName, values);

                        await PopulateSupportedValuesAndConfigurations(values);
                    }
                }
            }
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
