// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Query;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IPropertyPageQueryCacheFactory
    {
        internal static IPropertyPageQueryCache Create(
            IImmutableSet<ProjectConfiguration>? projectConfigurations = null,
            ProjectConfiguration? defaultConfiguration = null,
            Func<ProjectConfiguration, string, QueryProjectPropertiesContext, IRule>? bindToRule = null)
        {
            var mock = new Mock<IPropertyPageQueryCache>();

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
                    .BindToRule(It.IsAny<ProjectConfiguration>(), It.IsAny<string>(), It.IsAny<QueryProjectPropertiesContext>()))
                    .Returns((ProjectConfiguration config, string schema, QueryProjectPropertiesContext context) => Task.FromResult<IRule?>(bindToRule(config, schema, context)));
            }

            return mock.Object;
        }
    }
}
