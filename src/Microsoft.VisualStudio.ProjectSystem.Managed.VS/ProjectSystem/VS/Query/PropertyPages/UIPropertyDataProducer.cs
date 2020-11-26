// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyDataProducer
    {
        public static IEntityValue CreateUIPropertyValue(IEntityValue parent, IPropertyPageQueryCache cache, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(property, nameof(property));

            string propertyName = property.ContainingRule.PageTemplate == "commandNameBasedDebugger"
                ? DebugUtilities.ConvertRealPageAndPropertyToDebugProperty(property.ContainingRule.Name, property.Name)
                : property.Name;

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.UIPropertyName, propertyName)
                });

            return CreateUIPropertyValue(parent.EntityRuntime, identity, cache, property, order, requestedProperties);
        }

        public static IEntityValue CreateUIPropertyValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, IPropertyPageQueryCache cache, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(property, nameof(property));
            var newUIProperty = new UIPropertyValue(runtimeModel, id, new UIPropertyPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                if (property.ContainingRule.PageTemplate == "commandNameBasedDebugger")
                {
                    newUIProperty.Name = DebugUtilities.ConvertRealPageAndPropertyToDebugProperty(property.ContainingRule.Name, property.Name);
                }
                else
                {
                    newUIProperty.Name = property.Name;
                }
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

            if (requestedProperties.HelpUrl)
            {
                newUIProperty.HelpUrl = property.HelpUrl;
            }

            if (requestedProperties.CategoryName)
            {
                if (property.ContainingRule.PageTemplate == "commandNameBasedDebugger")
                {
                    newUIProperty.CategoryName = DebugUtilities.ConvertRealPageAndCategoryToDebugCategory(property.ContainingRule.Name, property.Category);
                }
                else
                {
                    newUIProperty.CategoryName = property.Category;
                }
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

                if (property.ContainingRule.PageTemplate == "commandNameBasedDebugger")
                {
                    dependsOnString = dependsOnString is not null
                        ? dependsOnString + ";"
                        : string.Empty;

                    dependsOnString = dependsOnString + "ParentDebugPropertyPage::ActiveLaunchProfile;ParentDebugPropertyPage::LaunchTarget";
                }

                newUIProperty.DependsOn = dependsOnString ?? string.Empty;
            }

            if (requestedProperties.VisibilityCondition)
            {
                string? visibilityCondition = property.GetMetadataValueOrNull("VisibilityCondition");

                if (property.ContainingRule.PageTemplate == "commandNameBasedDebugger"
                    && property.ContainingRule.Metadata.TryGetValue("CommandName", out object commandNameObject)
                    && commandNameObject is string commandName)
                {
                    var commandNameCondition = $"(eq (evaluated \"ParentDebugPropertyPage\" \"LaunchTarget\") \"{property.ContainingRule.Name}\")";
                    visibilityCondition = visibilityCondition is not null
                        ? $"(and {visibilityCondition} {commandNameCondition})"
                        : commandNameCondition;
                }

                if (visibilityCondition is null)
                {
                    newUIProperty.VisibilityCondition = string.Empty;
                }
                else
                {
                    newUIProperty.VisibilityCondition = visibilityCondition;
                }
            }

            ((IEntityValueFromProvider)newUIProperty).ProviderState = new PropertyProviderState(cache, property.ContainingRule, property.Name);

            return newUIProperty;
        }

        public static IEnumerable<IEntityValue> CreateUIPropertyValues(IEntityValue parent, IPropertyPageQueryCache cache, Rule rule, List<Rule>? childDebugRules, IUIPropertyPropertiesAvailableStatus properties)
        {
            foreach ((int index, BaseProperty property) in rule.Properties.WithIndices())
            {
                if (property.Visible)
                {
                    IEntityValue propertyValue = CreateUIPropertyValue(parent, cache, property, index, properties);
                    yield return propertyValue;
                }
            }

            if (childDebugRules is not null)
            {
                foreach (Rule childRule in childDebugRules)
                {
                    foreach ((int index, BaseProperty property) in childRule.Properties.WithIndices())
                    {
                        if (property.Visible)
                        {
                            IEntityValue propertyValue = CreateUIPropertyValue(parent, cache, property, index, properties);
                            yield return propertyValue;
                        }
                    }
                }
            }
        }

        public static async Task<IEntityValue?> CreateUIPropertyValueAsync(
            IEntityRuntimeModel runtimeModel,
            EntityIdentity requestId,
            IProjectService2 projectService,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            string path,
            string propertyPageName,
            string propertyName,
            IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            (propertyPageName, propertyName) = DebugUtilities.ConvertDebugPageAndPropertyToRealPageAndProperty(propertyPageName, propertyName);

            if (projectService.GetLoadedProject(path) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule
                && rule.TryGetPropertyAndIndex(propertyName, out BaseProperty? property, out int index)
                && property.Visible)
            {
                var context = queryCacheProvider.CreateCache(project);
                IEntityValue propertyValue = CreateUIPropertyValue(runtimeModel, requestId, context, property, index, requestedProperties);
                return propertyValue;
            }

            return null;
        }
    }
}
