// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
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
        private ImmutableList<string> _contexts = ImmutableList<string>.Empty;
        private string? _activeIntellisenseProjectContext;

        [ImportingConstructor]
        public ActiveEditorContextTracker(UnconfiguredProject? project) // For scoping
        {
        }

        public string? ActiveIntellisenseProjectContext
        {
            get { return _activeIntellisenseProjectContext ?? _contexts.FirstOrDefault(); }
            set { _activeIntellisenseProjectContext = value; }
        }

        public int GetProjectName(uint itemid, out string? pbstrProjectName)
        {
            if (itemid == HierarchyId.Nil || itemid == HierarchyId.Selection || itemid == HierarchyId.Empty)
            {
                pbstrProjectName = null;
                return HResult.InvalidArg;
            }

            pbstrProjectName = ActiveIntellisenseProjectContext;

            return HResult.OK;
        }

        public bool IsActiveEditorContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            if (!_contexts.Contains(contextId))
                throw new InvalidOperationException($"'{nameof(contextId)}' has not been registered or has already been unregistered");

            return StringComparers.WorkspaceProjectContextIds.Equals(ActiveIntellisenseProjectContext, contextId);
        }

        public void RegisterContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            bool changed = ThreadingTools.ApplyChangeOptimistically(ref _contexts, projectContexts =>
            {
                if (!projectContexts.Contains(contextId))
                {
                    projectContexts = projectContexts.Add(contextId);
                }

                return projectContexts;
            });

            if (!changed)
                throw new InvalidOperationException($"'{nameof(contextId)}' has already been registered.");
        }

        public void UnregisterContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            bool changed = ThreadingTools.ApplyChangeOptimistically(ref _contexts, projectContexts => projectContexts.Remove(contextId));

            if (!changed)
                throw new InvalidOperationException($"'{nameof(contextId)}' has not been registered or has already been unregistered");
        }
    }
}
