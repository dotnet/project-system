// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.TextManager.Interop;
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
        private ImmutableHashSet<string> _contexts = ImmutableHashSet<string>.Empty.WithComparer(StringComparers.WorkspaceProjectContextIds);
        private string? _activeIntellisenseProjectContext;

        [ImportingConstructor]
        public ActiveEditorContextTracker(UnconfiguredProject _) // For scoping
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
                throw new InvalidOperationException($"'{nameof(contextId)}' has not been registered or has already been unregistered.");

            return StringComparers.WorkspaceProjectContextIds.Equals(ActiveIntellisenseProjectContext, contextId);
        }

        public void RegisterContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            bool added = ImmutableInterlocked.Update(ref _contexts, static (contexts, id) => contexts.Add(id), contextId);

            if (!added)
                throw new InvalidOperationException($"'{nameof(contextId)}' has already been registered.");
        }

        public void UnregisterContext(string contextId)
        {
            Requires.NotNullOrEmpty(contextId, nameof(contextId));

            bool removed = ImmutableInterlocked.Update(ref _contexts, static (contexts, id) => contexts.Remove(id), contextId);

            if (!removed)
                throw new InvalidOperationException($"'{nameof(contextId)}' has not been registered or has already been unregistered.");
        }
    }
}
