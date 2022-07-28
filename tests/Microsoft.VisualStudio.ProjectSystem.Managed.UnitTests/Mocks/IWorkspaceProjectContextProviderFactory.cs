// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceProjectContextProviderFactory
    {
        public static IWorkspaceProjectContextProvider Create()
        {
            return new WorkspaceProjectContextProviderMock().Object;
        }

        public static IWorkspaceProjectContextProvider ImplementCreateProjectContextAsync(IWorkspaceProjectContextAccessor accessor)
        {
            var mock = new WorkspaceProjectContextProviderMock();

            mock.ImplementCreateProjectContextAsync(project => accessor);

            return mock.Object;
        }
    }
}
