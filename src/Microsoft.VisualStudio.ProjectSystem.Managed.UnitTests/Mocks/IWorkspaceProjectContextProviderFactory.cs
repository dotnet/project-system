// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceProjectContextProviderFactory
    {
        public static IWorkspaceProjectContextProvider Create()
        {
            return new WorkspaceProjectContextProviderMock().Object;
        }

        public static IWorkspaceProjectContextProvider ImplementCreateProjectContextAsync(IWorkspaceProjectContext context)
        {
            var mock = new WorkspaceProjectContextProviderMock();

            mock.ImplementCreateProjectContextAsync(project => context);

            return mock.Object;
        }
    }
}
