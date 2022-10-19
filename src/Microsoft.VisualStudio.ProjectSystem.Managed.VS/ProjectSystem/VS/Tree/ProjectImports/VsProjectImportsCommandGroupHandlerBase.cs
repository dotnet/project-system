// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.ProjectImports;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.ProjectImports
{
    /// <summary>
    /// Handles opening of files displayed in the project imports tree.
    /// </summary>
    internal abstract class VsProjectImportsCommandGroupHandlerBase : ProjectImportsCommandGroupHandlerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConfiguredProject _configuredProject;
        private readonly IVsUIService<IVsUIShellOpenDocument> _uiShellOpenDocument;
        private readonly IVsUIService<IVsExternalFilesManager> _externalFilesManager;
        private readonly IVsUIService<IOleServiceProvider> _oleServiceProvider;

        protected VsProjectImportsCommandGroupHandlerBase(
            IServiceProvider serviceProvider,
            ConfiguredProject configuredProject,
            IVsUIService<IVsUIShellOpenDocument> uiShellOpenDocument,
            IVsUIService<IVsExternalFilesManager> externalFilesManager,
            IVsUIService<IOleServiceProvider> oleServiceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(uiShellOpenDocument, nameof(uiShellOpenDocument));
            Requires.NotNull(externalFilesManager, nameof(externalFilesManager));
            Requires.NotNull(oleServiceProvider, nameof(oleServiceProvider));

            _serviceProvider = serviceProvider;
            _configuredProject = configuredProject;
            _uiShellOpenDocument = uiShellOpenDocument;
            _externalFilesManager = externalFilesManager;
            _oleServiceProvider = oleServiceProvider;
        }

        protected override void OpenItems(long commandId, IImmutableSet<IProjectTree> items)
        {
            Assumes.NotNull(_configuredProject.UnconfiguredProject.Services.HostObject);
            var hierarchy = (IVsUIHierarchy)_configuredProject.UnconfiguredProject.Services.HostObject;
            var rdt = new RunningDocumentTable(_serviceProvider);

            // Open all items.
            RunAllAndAggregateExceptions(items, OpenItem);

            void OpenItem(IProjectTree item)
            {
                IVsWindowFrame? windowFrame = null;
                try
                {
                    // Open the document.
                    Guid logicalView = IsOpenWithCommand(commandId) ? LOGVIEWID_UserChooseView : LOGVIEWID.Primary_guid;
                    IntPtr docData = IntPtr.Zero;

                    ErrorHandler.ThrowOnFailure(
                        _uiShellOpenDocument.Value.OpenStandardEditor(
                            (uint)__VSOSEFLAGS.OSE_ChooseBestStdEditor,
                            item.FilePath,
                            ref logicalView,
                            item.Caption,
                            hierarchy,
                            item.GetHierarchyId(),
                            docData,
                            _oleServiceProvider.Value,
                            out windowFrame));

                    RunningDocumentInfo rdtInfo = rdt.GetDocumentInfo(item.FilePath);

                    // Set it as read only if necessary.
                    bool isReadOnly = item.Flags.Contains(ImportTreeProvider.ProjectImportImplicit);

                    if (isReadOnly && rdtInfo.DocData is IVsTextBuffer textBuffer)
                    {
                        textBuffer.GetStateFlags(out uint flags);
                        textBuffer.SetStateFlags(flags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
                    }

                    // Detach the document from this project.
                    // Ignore failure. It may be that we've already transferred the item to Miscellaneous Files.
                    _externalFilesManager.Value.TransferDocument(item.FilePath, item.FilePath, windowFrame);

                    // Show the document window
                    if (windowFrame is not null)
                    {
                        ErrorHandler.ThrowOnFailure(windowFrame.Show());
                    }
                }
                catch
                {
                    windowFrame?.CloseFrame(0);
                    throw;
                }
            }
        }
    }
}
