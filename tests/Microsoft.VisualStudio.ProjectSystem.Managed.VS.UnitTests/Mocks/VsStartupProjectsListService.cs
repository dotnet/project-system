// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal class VsStartupProjectsListService : IVsStartupProjectsListService
    {
        public Guid? ProjectGuid { get; private set; }

        public void AddProject(ref Guid guidProject)
        {
            ProjectGuid = guidProject;
        }

        public void RemoveProject(ref Guid guidProject)
        {
            if (guidProject == ProjectGuid)
                ProjectGuid = null;
        }
    }
}
