// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextProvider
    {
        private class WorkspaceProjectContextAccessor : IWorkspaceProjectContextAccessor
        {
            public WorkspaceProjectContextAccessor(string contextId, IWorkspaceProjectContext context, IProjectThreadingService threadingService)
            {
                ContextId = contextId;

                // Wrap to enforce UI-thread
                Context = new ForegroundWorkspaceProjectContext(threadingService, context);
                HostSpecificEditAndContinueService = context;
                HostSpecificErrorReporter = context;
            }

            public string ContextId
            {
                get;
            }

            public IWorkspaceProjectContext Context
            {
                get;
            }

            public object HostSpecificEditAndContinueService
            {
                get;
            }

            public object HostSpecificErrorReporter
            {
                get;
            }
        }
    }
}
