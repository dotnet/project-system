// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;

namespace Microsoft.Build.Evaluation
{
    internal static class ProjectFactory
    {
        public static Project Create(ProjectRootElement rootElement)
        {
            return new Project(rootElement);
        }
    }
}
