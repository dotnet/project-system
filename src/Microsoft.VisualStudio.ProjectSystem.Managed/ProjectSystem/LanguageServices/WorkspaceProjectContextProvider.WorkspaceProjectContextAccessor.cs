// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
