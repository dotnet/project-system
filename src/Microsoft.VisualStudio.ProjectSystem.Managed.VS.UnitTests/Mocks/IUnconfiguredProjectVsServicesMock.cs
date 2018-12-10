// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IUnconfiguredProjectVsServicesMock : AbstractMock<IUnconfiguredProjectVsServices>
    {
        public IUnconfiguredProjectVsServicesMock ImplementVsHierarchy(IVsHierarchy hierarchy)
        {
            SetupGet(m => m.VsHierarchy)
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

        public IUnconfiguredProjectVsServicesMock ImplementActiveConfiguredProjectProperties(ProjectProperties projectProperties)
        {
            SetupGet(m => m.ActiveConfiguredProjectProperties)
                .Returns(projectProperties);

            return this;
        }
    }
}
