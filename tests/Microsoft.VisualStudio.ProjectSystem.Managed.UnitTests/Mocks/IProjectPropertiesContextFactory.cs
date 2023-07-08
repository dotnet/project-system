// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectPropertiesContextFactory
    {
        public static IProjectPropertiesContext Create(bool isProjectFile)
        {
            var projectItem = new Mock<IProjectPropertiesContext>();

            projectItem.SetupGet(o => o.IsProjectFile)
                .Returns(isProjectFile);

            return projectItem.Object;
        }
    }
}
