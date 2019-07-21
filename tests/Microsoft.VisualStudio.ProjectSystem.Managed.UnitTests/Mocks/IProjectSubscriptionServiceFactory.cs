// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSubscriptionServiceFactory
    {
        public static IProjectSubscriptionService Create()
        {
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            var ruleSource = ProjectValueDataSourceFactory.Create<IProjectSubscriptionUpdate>(services);
            var projectSource = ProjectValueDataSourceFactory.Create<IProjectSnapshot>(services);

            var mock = new Mock<IProjectSubscriptionService>();
            mock.SetupGet(s => s.ProjectRuleSource)
                .Returns(ruleSource);

            mock.SetupGet(s => s.ProjectBuildRuleSource)
                .Returns(ruleSource);

            mock.SetupGet(s => s.ProjectSource)
                .Returns(projectSource);

            mock.SetupGet(s => s.SourceItemsRuleSource)
                .Returns(ruleSource);

            return mock.Object;
        }
    }
}
