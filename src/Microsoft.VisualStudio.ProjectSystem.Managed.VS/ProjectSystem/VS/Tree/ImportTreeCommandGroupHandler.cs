// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Tree;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree
{
    /// <summary>
    /// Handles opening of files displayed in the import tree.
    /// </summary>
    [ExportCommandGroup(CMDSETID.UIHierarchyWindowCommandSet_string)]
    [AppliesTo(ProjectCapability.ProjectImportsTree)]
    [Order(ProjectSystem.Order.BeforeDefault)]
    internal sealed class ImportTreeCommandGroupHandler : IAsyncCommandGroupHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConfiguredProject _configuredProject;
        private readonly IVsUIService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> _uiShellOpenDocument;
        private readonly IVsUIService<IOleServiceProvider> _oleServiceProvider;

        [ImportingConstructor]
        public ImportTreeCommandGroupHandler(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ConfiguredProject configuredProject,
            IVsUIService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> uiShellOpenDocument,
            IVsUIService<IOleServiceProvider> oleServiceProvider)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(uiShellOpenDocument, nameof(uiShellOpenDocument));
            Requires.NotNull(oleServiceProvider, nameof(oleServiceProvider));

            _serviceProvider = serviceProvider;
            _configuredProject = configuredProject;
            _uiShellOpenDocument = uiShellOpenDocument;
            _oleServiceProvider = oleServiceProvider;
        }

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> items, long commandId, bool focused, string? commandText, CommandStatus status)
        {
            switch ((VsUIHierarchyWindowCmdIds)commandId)
            {
                case VsUIHierarchyWindowCmdIds.UIHWCMDID_DoubleClick:
                case VsUIHierarchyWindowCmdIds.UIHWCMDID_EnterKey:
                {
                    if (items.Count != 0 && items.All(CanOpenFile))
                    {
                        status |= CommandStatus.Enabled | CommandStatus.Supported;
                        return new CommandStatusResult(true, commandText, status).AsTask();
                    }

                    break;
                }
            }

            return CommandStatusResult.Unhandled.AsTask();
        }

        public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> items, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            switch ((VsUIHierarchyWindowCmdIds)commandId)
            {
                case VsUIHierarchyWindowCmdIds.UIHWCMDID_DoubleClick:
                case VsUIHierarchyWindowCmdIds.UIHWCMDID_EnterKey:
                {
                    if (items.Count != 0 && items.All(CanOpenFile))
                    {
                        OpenItems();

                        return TaskResult.True;
                    }

                    break;
                }
            }

            return TaskResult.False;

            void OpenItems()
            {
                IVsUIShellOpenDocument? uiShellOpenDocument = _uiShellOpenDocument.Value;
                Assumes.Present(uiShellOpenDocument);

                IOleServiceProvider? oleServiceProvider = _oleServiceProvider.Value;
                Assumes.Present(oleServiceProvider);

                var hierarchy = (IVsUIHierarchy) _configuredProject.UnconfiguredProject.Services.HostObject;
                var rdtHelper = new RunningDocumentTable(_serviceProvider);

                // Open all items
                RunAllAndAggregateExceptions(items, OpenItem);

                void OpenItem(IProjectTree item)
                {
                    IVsWindowFrame? windowFrame = null;
                    try
                    {
                        Guid logicalView = LOGVIEWID.Code_guid;
                        IntPtr docData = IntPtr.Zero;

                        ErrorHandler.ThrowOnFailure(
                            uiShellOpenDocument!.OpenStandardEditor(
                                (uint) __VSOSEFLAGS.OSE_ChooseBestStdEditor,
                                item.FilePath,
                                ref logicalView,
                                item.Caption,
                                hierarchy,
                                item.GetHierarchyId(),
                                docData,
                                oleServiceProvider,
                                out windowFrame));

                        bool isReadOnly = item.Flags.Contains(ProjectImportsSubTreeProvider.ProjectImportImplicit);

                        if (isReadOnly)
                        {
                            RunningDocumentInfo rdtInfo = rdtHelper.GetDocumentInfo(item.FilePath);
                            if (rdtInfo.DocData is IVsTextBuffer textBuffer)
                            {
                                textBuffer.GetStateFlags(out uint flags);
                                textBuffer.SetStateFlags(flags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
                            }
                        }

                        if (windowFrame != null)
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

        private static bool CanOpenFile(IProjectTree node) => node.Flags.Contains(ProjectImportsSubTreeProvider.ProjectImport);

        /// <summary>
        /// Calls <paramref name="action"/> for each of <paramref name="items"/>. If any action
        /// throws, its exception is caught and processing continues. When all items have been
        /// handled, any exceptions are thrown either as a single exception or an
        /// <see cref="AggregateException"/>.
        /// </summary>
        public static void RunAllAndAggregateExceptions<T>(IEnumerable<T> items, Action<T> action)
        {
            List<Exception>? exceptions = null;

            foreach (T item in items)
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null)
            {
                if (exceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(exceptions.First()).Throw();
                }
                else
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }
}
