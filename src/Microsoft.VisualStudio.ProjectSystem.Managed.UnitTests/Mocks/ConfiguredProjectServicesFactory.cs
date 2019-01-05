// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ConfiguredProjectServicesFactory
    {
        public static ConfiguredProjectServices Create(IPropertyPagesCatalogProvider propertyPagesCatalogProvider = null, IAdditionalRuleDefinitionsService ruleService = null, IProjectSubscriptionService projectSubscriptionService = null)
        {
            var mock = new Mock<ConfiguredProjectServices>();
            mock.Setup(c => c.PropertyPagesCatalog).Returns(propertyPagesCatalogProvider);
            mock.Setup(c => c.AdditionalRuleDefinitions).Returns(ruleService);
            mock.Setup(c => c.ProjectSubscription).Returns(projectSubscriptionService);
            return mock.Object;
        }
    }
}
