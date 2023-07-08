// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
