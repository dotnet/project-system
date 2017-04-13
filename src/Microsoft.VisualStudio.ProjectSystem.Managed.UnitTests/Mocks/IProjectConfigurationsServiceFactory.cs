// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Moq;

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
