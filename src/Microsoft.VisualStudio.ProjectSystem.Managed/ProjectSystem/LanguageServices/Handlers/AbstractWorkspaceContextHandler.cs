// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    internal class AbstractWorkspaceContextHandler : IWorkspaceContextHandler
    {
        private IWorkspaceProjectContext _context;

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
            if (_context != null)
                throw new InvalidOperationException();

            _context = context;
        }

        protected void EnsureInitialized()
        {
            if (_context == null)
                throw new InvalidOperationException("Initialize must be called before calling this method.");
        }
    }
}
