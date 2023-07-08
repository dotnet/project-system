// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IActiveConfiguredProjectSubscriptionServiceFactory
    {
        public static IActiveConfiguredProjectSubscriptionService Create(IProjectValueDataSource<IProjectSubscriptionUpdate>? sourceItemsRuleSource = null)
        {
            var mock = new Mock<IActiveConfiguredProjectSubscriptionService>();

            mock.SetupGet(s => s.ProjectRuleSource)
                .Returns(IProjectValueDataSourceFactory.CreateInstance<IProjectSubscriptionUpdate>);

            if (sourceItemsRuleSource is not null)
            {
                mock.SetupGet(s => s.SourceItemsRuleSource)
                    .Returns(() => sourceItemsRuleSource);
            }

            return mock.Object;
        }
    }
}
