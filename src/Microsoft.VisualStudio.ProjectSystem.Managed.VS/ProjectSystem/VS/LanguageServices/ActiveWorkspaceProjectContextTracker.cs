// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Provides an implementation of <see cref="IActiveWorkspaceProjectContextTracker"/> that tracks the 
    ///     "active" context by handling the VSHPROPID_ActiveIntellisenseProjectContext IVsHierarchy property.
    /// </summary>
    [Export(typeof(IActiveIntellisenseProjectProvider))]
    [ExportProjectNodeComService(typeof(IVsContainedLanguageProjectNameProvider))]
    [AppliesTo(ProjectCapability.DotNetLanguageService2)]
    internal class ActiveWorkspaceProjectContextTracker : IActiveIntellisenseProjectProvider, IVsContainedLanguageProjectNameProvider, IActiveWorkspaceProjectContextTracker
    {
        private ImmutableDictionary<IWorkspaceProjectContext, string> _contexts = ImmutableDictionary<IWorkspaceProjectContext, string>.Empty;

        [ImportingConstructor]
        public ActiveWorkspaceProjectContextTracker(UnconfiguredProject project) // For scoping
        {
        }

        public string ActiveIntellisenseProjectContext
        {
            get;
            set;
        }

        public int GetProjectName(uint itemid, out string pbstrProjectName)
        {
            if (itemid == HierarchyId.Nil || itemid == HierarchyId.Selection || itemid == HierarchyId.Empty)
            {
                pbstrProjectName = null;
                return HResult.InvalidArg;
            }

            pbstrProjectName = ActiveIntellisenseProjectContext;

            return HResult.OK;
        }

        public bool IsActiveContext(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (_contexts.TryGetValue(context, out string projectContextId))
            {
                return StringComparers.WorkspaceProjectContextIds.Equals(ActiveIntellisenseProjectContext, projectContextId);
            }

            throw new InvalidOperationException("'context' has not been registered or has already been unregistered");
        }

        public void RegisterContext(IWorkspaceProjectContext context, string contextId)
        {
            Requires.NotNull(context, nameof(context));
            Requires.NotNullOrEmpty(contextId, nameof(contextId));
            
            bool changed = ThreadingTools.ApplyChangeOptimistically(ref _contexts,
                                                                    projectContexts => projectContexts.SetItem(context, contextId));

            if (!changed)
                throw new InvalidOperationException("'context' has already been registered.");
        }

        public void UnregisterContext(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            bool changed = ThreadingTools.ApplyChangeOptimistically(ref _contexts,
                                                                    projectContexts => projectContexts.Remove(context));

            if (!changed)
                throw new InvalidOperationException("'context' has not been registered or has already been unregistered");
        }
    }
}
