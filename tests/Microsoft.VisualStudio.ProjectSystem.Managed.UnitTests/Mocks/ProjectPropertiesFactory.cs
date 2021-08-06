// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectPropertiesFactory
    {
        public static ProjectProperties CreateEmpty()
        {
            return Create(UnconfiguredProjectFactory.Create());
        }

        public static ProjectProperties Create(string category, string projectXml, (Type type, string name)[] properties)
        {
            var rootElement = ProjectRootElementFactory.Create(projectXml);
            var project = ProjectFactory.Create(rootElement);

            var data = new List<PropertyPageData>();

            foreach (var (type, name) in properties)
            {
                var property = project.GetProperty(name);
                if (property == null)
                {
                    data.Add(new PropertyPageData(category, name, string.Empty, propertyType: type));
                }
                else
                {
                    data.Add(new PropertyPageData(category, name, property.EvaluatedValue, propertyType: type));
                }
            }


            return Create(data.ToArray());
        }

        public static ProjectProperties Create(string category, string propertyName, string value)
        {
            var data = new PropertyPageData(category, propertyName, value);

            return Create(data);
        }

        public static ProjectProperties Create(params PropertyPageData[] data)
        {
            return Create(UnconfiguredProjectFactory.Create(), data);
        }

        public static ProjectProperties Create(UnconfiguredProject project, params PropertyPageData[] data)
        {
            var catalog = IPropertyPagesCatalogFactory.Create(data);
            var propertyPagesCatalogProvider = IPropertyPagesCatalogProviderFactory.Create(
                    new Dictionary<string, IPropertyPagesCatalog>
                    {
                        { "Project", catalog }
                    },
                    catalog
                );

            IAdditionalRuleDefinitionsService ruleService = Mock.Of<IAdditionalRuleDefinitionsService>();

            var configuredProjectServices = ConfiguredProjectServicesFactory.Create(
                propertyPagesCatalogProvider: propertyPagesCatalogProvider,
                ruleService: ruleService);

            var cfg = new StandardProjectConfiguration("Debug|" + "AnyCPU", Empty.PropertiesMap.SetItem("Configuration", "Debug").SetItem("Platform", "AnyCPU"));
            ConfiguredProject configuredProject = Mock.Of<ConfiguredProject>(o =>
                o.UnconfiguredProject == project &&
                o.Services == configuredProjectServices &&
                o.ProjectConfiguration == cfg);

            return new ProjectProperties(configuredProject);
        }
    }
}
