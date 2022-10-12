// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides an implementation of <see cref="IActiveEditorContextTracker"/> that tracks the "active" context for the editor.
    /// </summary>
    [Export(typeof(IActiveEditorContextTracker))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class ActiveEditorContextTracker : IActiveEditorContextTracker
    {
        private ImmutableList<string> _contexts = ImmutableList<string>.Empty;
        private string? _activeIntellisenseProjectContext;

        // UnconfiguredProject is only included for scoping reasons.
        [ImportingConstructor]
        public ActiveEditorContextTracker(UnconfiguredProject? project)
        {
        }

        public string? ActiveIntellisenseProjectContext
        {
            get { return _activeIntellisenseProjectContext ?? _contexts.FirstOrDefault(); }
            set { _activeIntellisenseProjectContext = value; }
        }

        public bool IsActiveEditorContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            if (!_contexts.Contains(contextId))
            {
                throw new InvalidOperationException($"Context with ID '{contextId}' has not been registered or has already been unregistered.");
            }

            return StringComparers.WorkspaceProjectContextIds.Equals(ActiveIntellisenseProjectContext, contextId);
        }

        public IDisposable RegisterContext(string contextId)
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
            {
                throw new InvalidOperationException($"Context with ID '{contextId}' has already been registered.");
            }

            return new DisposableDelegate(() =>
                Assumes.True(ThreadingTools.ApplyChangeOptimistically(ref _contexts, contextId, static (projectContexts, contextId) =>
                    projectContexts.Remove(contextId))));
        }
    }
}
