// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.TextManager.Interop;
using HierarchyId = Microsoft.VisualStudio.Shell.HierarchyId;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Tracks the "active" context for the editor by handling the VSHPROPID_ActiveIntellisenseProjectContext hierarchy property.
    /// </summary>
    [Export(typeof(IActiveIntellisenseProjectProvider))]
    [ExportProjectNodeComService(typeof(IVsContainedLanguageProjectNameProvider))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class VsActiveEditorContextTracker : IActiveIntellisenseProjectProvider, IVsContainedLanguageProjectNameProvider
    {
        private readonly IActiveEditorContextTracker _activeEditorContextTracker;

        // UnconfiguredProject is only included for scoping reasons.
        [ImportingConstructor]
        public VsActiveEditorContextTracker(UnconfiguredProject? project, IActiveEditorContextTracker activeEditorContextTracker)
        {
            _activeEditorContextTracker = activeEditorContextTracker;
        }

        public string? ActiveIntellisenseProjectContext
        {
            get { return _activeEditorContextTracker.ActiveIntellisenseProjectContext; }
            set { _activeEditorContextTracker.ActiveIntellisenseProjectContext = value; }
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

        // Pass-through for unit testing.
        internal bool IsActiveEditorContext(string contextId) => _activeEditorContextTracker.IsActiveEditorContext(contextId);

        // Pass-through for unit testing.
        internal IDisposable RegisterContext(string contextId) => _activeEditorContextTracker.RegisterContext(contextId);
    }
}
