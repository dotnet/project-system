// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class WorkspaceProjectContextProviderMock : AbstractMock<IWorkspaceProjectContextProvider>
    {
        public WorkspaceProjectContextProviderMock ImplementCreateProjectContextAsync(Func<ConfiguredProject, IWorkspaceProjectContext> action)
        {
            Setup(m => m.CreateProjectContextAsync(It.IsAny<ConfiguredProject>()))
              .ReturnsAsync(action);

            return this;
        }

        public WorkspaceProjectContextProviderMock ImplementReleaseProjectContextAsync(Action<IWorkspaceProjectContext> action)
        {
            Setup(m => m.ReleaseProjectContextAsync(It.IsAny<IWorkspaceProjectContext>()))
              .ReturnsAsync(action);

            return this;
        }
    }
}
