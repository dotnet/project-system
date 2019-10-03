// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextProvider
    {
        private class WorkspaceProjectContextAccessor : IWorkspaceProjectContextAccessor
        {
            private readonly IWorkspaceProjectContext _context;
            private readonly string _contextId;

            public WorkspaceProjectContextAccessor(string contextId, IWorkspaceProjectContext context)
            {
                _contextId = contextId;
                _context = context;
            }

            public string ContextId
            {
                get { return _contextId; }
            }

            public IWorkspaceProjectContext Context
            {
                get { return _context; }
            }

            public object HostSpecificErrorReporter
            {
                get { return _context; }
            }
        }
    }
}
