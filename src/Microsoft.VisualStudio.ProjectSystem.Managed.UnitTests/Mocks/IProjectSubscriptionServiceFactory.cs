// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSubscriptionServiceFactory
    {
        public static IProjectSubscriptionService Create()
        {
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            var source = ProjectValueDataSourceFactory.Create<IProjectSubscriptionUpdate>(services);

            var mock = new Mock<IProjectSubscriptionService>();
            mock.SetupGet(s => s.ProjectRuleSource)
                .Returns(source);

            mock.SetupGet(s => s.ProjectBuildRuleSource)
                .Returns(source);

            return mock.Object;
        }
    }
}
