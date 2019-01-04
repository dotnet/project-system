// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

using HierarchyId = Microsoft.VisualStudio.Shell.HierarchyId;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Provides an implementation of <see cref="IActiveEditorContextTracker"/> that tracks the 
    ///     "active" context for the editor by handling the VSHPROPID_ActiveIntellisenseProjectContext 
    ///     hierarchy property.
    /// </summary>
    [Export(typeof(IActiveIntellisenseProjectProvider))]
    [Export(typeof(IActiveEditorContextTracker))]
    [ExportProjectNodeComService(typeof(IVsContainedLanguageProjectNameProvider))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class ActiveEditorContextTracker : IActiveIntellisenseProjectProvider, IVsContainedLanguageProjectNameProvider, IActiveEditorContextTracker
    {
        private readonly IActiveWorkspaceProjectContextHost _activeWorkspaceProjectContextHost;
        private readonly IProjectThreadingService _threadingService;
        private ImmutableDictionary<IWorkspaceProjectContext, string> _contexts = ImmutableDictionary<IWorkspaceProjectContext, string>.Empty;
        private string _activeIntellisenseProjectContext;

        [ImportingConstructor]
        public ActiveEditorContextTracker(IProjectThreadingService threadingService, IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost)
        {
            _threadingService = threadingService;
            _activeWorkspaceProjectContextHost = activeWorkspaceProjectContextHost;
        }

        public string ActiveIntellisenseProjectContext
        {
            get { return _activeIntellisenseProjectContext ?? GetWorkspaceContextIdFromActiveContext(); }
            set { _activeIntellisenseProjectContext = value; }
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

        public bool IsActiveEditorContext(IWorkspaceProjectContext context)
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

        private string GetWorkspaceContextIdFromActiveContext()
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                try
                {
                    // If we're never been set an active context, we just
                    // pick one based on the active configuration.
                    return await _activeWorkspaceProjectContextHost.OpenContextForWriteAsync(a => Task.FromResult(a.ContextId));
                }
                catch (OperationCanceledException)
                {   // Project unloading

                    return null;
                }
            });
        }
    }
}
