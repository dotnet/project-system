// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
