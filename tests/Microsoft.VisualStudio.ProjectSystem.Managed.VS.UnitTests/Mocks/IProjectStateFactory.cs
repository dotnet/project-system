// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Query;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IProjectStateFactory
    {
        internal static IProjectState Create(
            IImmutableSet<ProjectConfiguration>? projectConfigurations = null,
            ProjectConfiguration? defaultConfiguration = null,
            Func<ProjectConfiguration, string, QueryProjectPropertiesContext, IRule>? bindToRule = null)
        {
            var mock = new Mock<IProjectState>();

            if (projectConfigurations is not null)
            {
                mock.Setup(cache => cache.GetKnownConfigurationsAsync()).ReturnsAsync(projectConfigurations);
            }

            if (defaultConfiguration is not null)
            {
                mock.Setup(cache => cache.GetSuggestedConfigurationAsync()).ReturnsAsync(defaultConfiguration);
            }

            if (bindToRule is not null)
            {
                mock.Setup(cache => cache
                    .BindToRuleAsync(It.IsAny<ProjectConfiguration>(), It.IsAny<string>(), It.IsAny<QueryProjectPropertiesContext>()))
                    .Returns((ProjectConfiguration config, string schema, QueryProjectPropertiesContext context) => Task.FromResult<IRule?>(bindToRule(config, schema, context)));
            }

            return mock.Object;
        }
    }
}
