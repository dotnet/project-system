// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPageSnapshot"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyDataProducer
    {
        public static IEntityValue CreateUIPropertyValue(IQueryExecutionContext queryExecutionContext, IEntityValue parent, IProjectState cache, QueryProjectPropertiesContext propertiesContext, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.UIPropertyName, property.Name)
                });

            return CreateUIPropertyValue(queryExecutionContext, identity, cache, propertiesContext, property, order, requestedProperties);
        }

        public static IEntityValue CreateUIPropertyValue(IQueryExecutionContext queryExecutionContext, EntityIdentity id, IProjectState cache, QueryProjectPropertiesContext propertiesContext, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(property, nameof(property));
            var newUIProperty = new UIPropertySnapshot(queryExecutionContext.EntityRuntime, id, new UIPropertyPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newUIProperty.Name = property.Name;
            }

            if (requestedProperties.DisplayName)
            {
                newUIProperty.DisplayName = property.DisplayName;
            }

            if (requestedProperties.Description)
            {
                newUIProperty.Description = property.Description;
            }

            if (requestedProperties.ConfigurationIndependent)
            {
                newUIProperty.ConfigurationIndependent = !property.IsConfigurationDependent();
            }

            if (requestedProperties.IsReadOnly)
            {
                newUIProperty.IsReadOnly = property.ReadOnly;
            }

            if (requestedProperties.IsVisible)
            {
                newUIProperty.IsVisible = property.Visible;
            }

            if (requestedProperties.HelpUrl)
            {
                newUIProperty.HelpUrl = property.HelpUrl;
            }

            if (requestedProperties.CategoryName)
            {
                newUIProperty.CategoryName = property.Category;
            }

            if (requestedProperties.Order)
            {
                newUIProperty.Order = order;
            }

            if (requestedProperties.Type)
            {
                newUIProperty.Type = property switch
                {
                    IntProperty => "int",
                    BoolProperty => "bool",
                    EnumProperty => "enum",
                    DynamicEnumProperty => "enum",
                    StringListProperty => "list",
                    _ => "string"
                };
            }

            if (requestedProperties.SearchTerms)
            {
                string? searchTermsString = property.GetMetadataValueOrNull("SearchTerms");
                newUIProperty.SearchTerms = searchTermsString ?? string.Empty;
            }

            if (requestedProperties.DependsOn)
            {
                string? dependsOnString = property.GetMetadataValueOrNull("DependsOn");
                newUIProperty.DependsOn = dependsOnString ?? string.Empty;
            }

            if (requestedProperties.VisibilityCondition)
            {
                string? visibilityCondition = property.GetMetadataValueOrNull("VisibilityCondition");
                newUIProperty.VisibilityCondition = visibilityCondition ?? string.Empty;
            }

            if (requestedProperties.DimensionVisibilityCondition)
            {
                string? dimensionVisibilityCondition = property.GetMetadataValueOrNull("DimensionVisibilityCondition");
                newUIProperty.DimensionVisibilityCondition = dimensionVisibilityCondition ?? string.Empty;
            }
            
            
            if (requestedProperties.ConfiguredValueVisibilityCondition)
            {
                string? configuredValueVisibilityCondition = property.GetMetadataValueOrNull("ConfiguredValueVisibilityCondition");
                newUIProperty.ConfiguredValueVisibilityCondition = configuredValueVisibilityCondition ?? string.Empty;
            }
            
            if (requestedProperties.IsReadOnlyCondition)
            {
                string? isReadOnlyCondition = property.GetMetadataValueOrNull("IsReadOnlyCondition");
                newUIProperty.IsReadOnlyCondition = isReadOnlyCondition ?? string.Empty;
            }
            
            ((IEntityValueFromProvider)newUIProperty).ProviderState = new PropertyProviderState(cache, property.ContainingRule, propertiesContext, property.Name);

            return newUIProperty;
        }

        public static IEnumerable<IEntityValue> CreateUIPropertyValues(IQueryExecutionContext queryExecutionContext, IEntityValue parent, IProjectState cache, QueryProjectPropertiesContext propertiesContext, Rule rule, IUIPropertyPropertiesAvailableStatus properties)
        {
            foreach ((int index, BaseProperty property) in rule.Properties.WithIndices())
            {
                IEntityValue propertyValue = CreateUIPropertyValue(queryExecutionContext, parent, cache, propertiesContext, property, index, properties);
                yield return propertyValue;
            }
        }

        public static async Task<IEntityValue?> CreateUIPropertyValueAsync(
            IQueryExecutionContext queryExecutionContext,
            EntityIdentity requestId,
            IProjectService2 projectService,
            QueryProjectPropertiesContext propertiesContext,
            string propertyPageName,
            string propertyName,
            IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            if (projectService.GetLoadedProject(propertiesContext.File) is UnconfiguredProject project)
            {
                project.GetQueryDataVersion(out string versionKey, out long versionNumber);
                queryExecutionContext.ReportInputDataVersion(versionKey, versionNumber);

                if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                    && projectCatalog.GetSchema(propertyPageName) is Rule rule
                    && rule.TryGetPropertyAndIndex(propertyName, out BaseProperty? property, out int index)
                    && property.Visible)
                {
                    IProjectState? projectState = null;
                    if (StringComparers.ItemTypes.Equals(propertiesContext.ItemType, "LaunchProfile"))
                    {
                        if (project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider
                            && project.Services.ExportProvider.GetExportedValueOrDefault<LaunchSettingsTracker>() is LaunchSettingsTracker launchSettingsTracker)
                        {
                            projectState = new LaunchProfileProjectState(project, launchSettingsProvider, launchSettingsTracker);
                        }
                    }
                    else if (propertiesContext.IsProjectFile)
                    {
                        projectState = new PropertyPageProjectState(project);
                    }

                    if (projectState is not null)
                    {
                        IEntityValue propertyValue = CreateUIPropertyValue(queryExecutionContext, requestId, projectState, propertiesContext, property, index, requestedProperties);
                        return propertyValue;
                    }
                }
            }

            return null;
        }
    }
}
