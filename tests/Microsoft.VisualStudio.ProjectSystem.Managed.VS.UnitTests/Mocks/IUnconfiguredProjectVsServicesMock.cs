// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IUnconfiguredProjectVsServicesMock : AbstractMock<IUnconfiguredProjectVsServices>
    {
        public IUnconfiguredProjectVsServicesMock ImplementVsHierarchy(IVsHierarchy? hierarchy)
        {
            SetupGet<IVsHierarchy?>(m => m.VsHierarchy)
                .Returns(hierarchy);

            return this;
        }

        public IUnconfiguredProjectVsServicesMock ImplementVsProject(IVsProject4 project)
        {
            SetupGet(m => m.VsProject)
                .Returns(project);

            return this;
        }

        public IUnconfiguredProjectVsServicesMock ImplementThreadingService(IProjectThreadingService threadingService)
        {
            SetupGet(m => m.ThreadingService)
                .Returns(threadingService);

            return this;
        }

        public IUnconfiguredProjectVsServicesMock ImplementActiveConfiguredProjectProperties(ProjectProperties? projectProperties)
        {
            SetupGet<ProjectProperties?>(m => m.ActiveConfiguredProjectProperties)
                .Returns(projectProperties);

            return this;
        }
    }
}
