// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class WorkspaceProjectContextProviderMock : AbstractMock<IWorkspaceProjectContextProvider>
    {
        public WorkspaceProjectContextProviderMock ImplementCreateProjectContextAsync(Func<ConfiguredProject, IWorkspaceProjectContextAccessor> action)
        {
            Setup(m => m.CreateProjectContextAsync(It.IsAny<ConfiguredProject>()))
              .ReturnsAsync(action);

            return this;
        }

        public WorkspaceProjectContextProviderMock ImplementReleaseProjectContextAsync(Action<IWorkspaceProjectContextAccessor> action)
        {
            Setup(m => m.ReleaseProjectContextAsync(It.IsAny<IWorkspaceProjectContextAccessor>()))
              .ReturnsAsync(action);

            return this;
        }
    }
}
