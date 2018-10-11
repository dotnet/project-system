// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextProvider
    {
        private class WorkspaceProjectContextAccessor : IWorkspaceProjectContextAccessor
        {
            private readonly IWorkspaceProjectContext _context;
            private readonly IProjectThreadingService _threadingService;
            private readonly string _contextId;

            public WorkspaceProjectContextAccessor(string contextId, IWorkspaceProjectContext context, IProjectThreadingService threadingService)
            {
                _contextId = contextId;
                _context = context;
                _threadingService = threadingService;
            }

            public string ContextId
            {
                get { return _contextId; }
            }

            public IWorkspaceProjectContext Context
            {
                get
                {
                    _threadingService.VerifyOnUIThread();

                    return _context;
                }
            }

            public object HostSpecificEditAndContinueService
            {
                get { return Context; }
            }

            public object HostSpecificErrorReporter
            {
                get { return Context; }
            }
        }
    }
}
