// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemFactory
    {
        public static IProjectItem Create(string evaluatedInclude, IProjectProperties properties, bool isProjectFile = true)
        {
            var projectItem = new Mock<IProjectItem>();

            projectItem.SetupGet(o => o.EvaluatedInclude)
                .Returns(evaluatedInclude);
            projectItem.SetupGet(o => o.Metadata)
                .Returns(properties);

            projectItem.Setup(o => o.SetUnevaluatedIncludeAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            projectItem.Setup(o => o.RemoveAsync(It.IsAny<DeleteOptions>()))
                .Returns(Task.CompletedTask);
            var propertiesContext = IProjectPropertiesContextFactory.Create(isProjectFile);
            projectItem.SetupGet(o => o.PropertiesContext)
                .Returns(propertiesContext);

            return projectItem.Object;
        }
    }
}
