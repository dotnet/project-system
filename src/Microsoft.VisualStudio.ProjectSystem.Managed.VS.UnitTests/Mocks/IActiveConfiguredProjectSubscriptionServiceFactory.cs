// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IActiveConfiguredProjectSubscriptionServiceFactory
    {
        public static IActiveConfiguredProjectSubscriptionService Create(IProjectValueDataSource<IProjectSubscriptionUpdate>? sourceItemsRuleSource = null)
        {
            var mock = new Mock<IActiveConfiguredProjectSubscriptionService>();

            mock.SetupGet(s => s.ProjectRuleSource)
                .Returns(() => IProjectValueDataSourceFactory.CreateInstance<IProjectSubscriptionUpdate>());

            if (sourceItemsRuleSource != null)
            {
                mock.SetupGet(s => s.SourceItemsRuleSource)
                    .Returns(() => sourceItemsRuleSource);
            }

            return mock.Object;
        }
    }
}
