// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectConfigurationsServiceFactory
    {
        public static IProjectConfigurationsService ImplementGetKnownProjectConfigurationsAsync(IImmutableSet<ProjectConfiguration> action)
        {
            var mock = new Mock<IProjectConfigurationsService>();
            mock.Setup(p => p.GetKnownProjectConfigurationsAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
