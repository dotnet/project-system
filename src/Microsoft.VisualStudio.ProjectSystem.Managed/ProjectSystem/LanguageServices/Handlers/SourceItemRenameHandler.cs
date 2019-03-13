// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.ProjectSystem.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Export(typeof(IWorkspaceContextHandler))]
    internal class SourceItemRenameHandler : AbstractWorkspaceContextHandler, IProjectUpdatedHandler
    {
        private readonly IFileRenameHandler[] _renameHandlers;
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public SourceItemRenameHandler(UnconfiguredProject project, [ImportMany]IFileRenameHandler[] renameHandlers)
        {
            _renameHandlers = renameHandlers;
            _project = project;
        }

        public string ProjectUpdatedRule
        {
            get { return Compile.SchemaName; }
        }

        public void HandleUpdate(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            foreach (IFileRenameHandler hander in _renameHandlers)
            {
                foreach ((string oldFilePath, string newFilePath) in projectChange.Difference.RenamedItems)
                {
                    hander.HandleRename(
                        _project.MakeRooted(oldFilePath),
                        _project.MakeRooted(newFilePath));
                }
            }
        }
    }
}
