// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemFactory
    {
        public static IProjectItem Create(string evaluatedInclude, IProjectProperties properties)
        {
            var projectItem = new Mock<IProjectItem>();

            projectItem.SetupGet(o => o.EvaluatedInclude)
                .Returns(evaluatedInclude);
            projectItem.SetupGet(o => o.Metadata)
                .Returns(properties);

            return projectItem.Object;
        }
    }
}
