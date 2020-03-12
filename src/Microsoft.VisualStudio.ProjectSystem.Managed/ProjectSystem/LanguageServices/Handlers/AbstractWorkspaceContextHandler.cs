// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    internal class AbstractWorkspaceContextHandler : IWorkspaceContextHandler
    {
        private IWorkspaceProjectContext? _context;

        protected AbstractWorkspaceContextHandler()
        {
        }

        protected IWorkspaceProjectContext Context
        {
            get
            {
                Assumes.NotNull(_context);

                return _context;
            }
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (_context != null)
                throw new InvalidOperationException();

            _context = context;
        }

        protected void VerifyInitialized()
        {
            Verify.Operation(_context != null, "Must call Initialize(IWorkspaceProjectContext) first.");
        }
    }
}
