// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ConfiguredProjectServicesFactory
    {
        public static ConfiguredProjectServices Create(IPropertyPagesCatalogProvider? propertyPagesCatalogProvider = null,
                                                       IAdditionalRuleDefinitionsService? ruleService = null,
                                                       IProjectSubscriptionService? projectSubscriptionService = null,
                                                       IProjectPropertiesProvider? projectPropertiesProvider = null,
                                                       IProjectService? projectService = null,
                                                       IProjectThreadingService? threadingService = null,
                                                       IProjectAsynchronousTasksService? projectAsynchronousTasksService = null)
        {
            var mock = new Mock<ConfiguredProjectServices>();
            mock.Setup(c => c.PropertyPagesCatalog).Returns(propertyPagesCatalogProvider!);
            mock.Setup(c => c.AdditionalRuleDefinitions).Returns(ruleService!);
            mock.Setup(c => c.ProjectSubscription).Returns(projectSubscriptionService!);
            mock.Setup(c => c.ProjectPropertiesProvider).Returns(projectPropertiesProvider!);
            mock.Setup(c => c.ThreadingPolicy).Returns(threadingService!);
            mock.Setup(c => c.ProjectAsynchronousTasks).Returns(projectAsynchronousTasksService!);
            mock.SetupGet(s => s.ProjectService).Returns(projectService!);
            return mock.Object;
        }
    }
}
