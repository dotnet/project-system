// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Definition;

namespace Microsoft.Build.Execution
{
    internal static class ProjectInstanceFactory
    {
        public static ProjectInstance Create(string? xml = null)
        {
            var element = ProjectRootElementFactory.Create(xml);

            return ProjectInstance.FromProjectRootElement(element, new ProjectOptions());
        }
    }
}
