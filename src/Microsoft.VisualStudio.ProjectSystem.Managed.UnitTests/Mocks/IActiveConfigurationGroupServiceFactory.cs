// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfigurationGroupServiceFactory
    {
        public static IActiveConfigurationGroupService Implement(IProjectValueDataSource<IConfigurationGroup<ProjectConfiguration>> source)
        {
            var mock = new Mock<IActiveConfigurationGroupService>();
            mock.SetupGet(s => s.ActiveConfigurationGroupSource)
                .Returns(source);

            return mock.Object;
        }
    }
}
