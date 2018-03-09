// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using InternalUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal abstract class TableToolWindow : ToolWindowPane, IOleCommandTarget
    {
        public const string SearchFilterKey = "SearchFilter";

        private readonly ContentWrapper _contentWrapper;
        protected bool IsDisposed;

        protected IWpfTableControl2 TableControl { get; private set; }

        public override bool SearchEnabled => true;

        protected abstract string SearchWatermark { get; }

        protected abstract string WindowCaption { get; }

        protected abstract int ToolbarMenuId { get; }

        protected abstract int ContextMenuId { get; }

        protected TableToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = WindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGuid, ToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            _contentWrapper = new ContentWrapper(ContextMenuId);
            Content = _contentWrapper;
        }

        private static void OnFiltersChanged(object sender, FiltersChangedEventArgs e) =>
            ProjectSystemToolsPackage.UpdateQueryStatus();

        private static void OnGroupingsChanged(object sender, EventArgs e) =>
            ProjectSystemToolsPackage.UpdateQueryStatus();

        protected virtual void SetTableControl(IWpfTableControl2 tableControl)
        {
            if (TableControl != null)
            {
                TableControl.FiltersChanged -= OnFiltersChanged;
                TableControl.GroupingsChanged -= OnGroupingsChanged;
                _contentWrapper.Child = null;
                TableControl.Dispose();
            }

            TableControl = tableControl;

            if (tableControl != null)
            {
                _contentWrapper.Child = TableControl.Control;
                TableControl.FiltersChanged += OnFiltersChanged;
                TableControl.GroupingsChanged += OnGroupingsChanged;
            }
        }

        public override IVsSearchTask CreateSearch([ComAliasName("VsShell.VSCOOKIE")]uint dwCookie, IVsSearchQuery pSearchQuery,
                                                   IVsSearchCallback pSearchCallback)
        {
            if (TableControl == null)
            {
                System.Diagnostics.Debug.Fail("Attempting to search before initializing Error or Task ListWindow");
                throw new InvalidOperationException("Attempting to search before initializing Error or Task ListWindow");
            }

            return new TableSearchTask(dwCookie, pSearchQuery, pSearchCallback, TableControl);
        }

        public override void ClearSearch()
        {
            if (TableControl != null)
            {
                TableControl.SetFilter(SearchFilterKey, null);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Attempting to clear before initializing ErrorListWindow");
            }
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, (uint)200);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartType, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartDelay, (uint)100);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchUseMRU, true);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.PrefixFilterMRUItems, false);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.MaximumMRUItems, (uint)25);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchWatermark, SearchWatermark);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchPopupAutoDropdown, false);
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlBorderThickness, "1");
            InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchProgressType, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
        }

        protected override object GetService(Type serviceType) =>
            serviceType.IsEquivalentTo(typeof(IOleCommandTarget)) ? this : base.GetService(serviceType);

        protected abstract int InnerQueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands,
            IntPtr commandText);

        protected abstract int InnerExec(ref Guid commandGroupGuid, uint commandId, uint commandExecOption,
            IntPtr pvaIn, IntPtr pvaOut);

        int IOleCommandTarget.QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
            var result = InnerQueryStatus(ref commandGroupGuid, commandCount, commands, commandText);

            if (result == VSConstants.S_OK)
            {
                return result;
            }

            return ((IOleCommandTarget)TableControl).QueryStatus(ref commandGroupGuid, commandCount, commands, commandText);
        }

        int IOleCommandTarget.Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecOption, IntPtr pvaIn, IntPtr pvaOut)
        {
            var result = InnerExec(ref commandGroupGuid, commandId, commandExecOption, pvaIn, pvaOut);

            if (result == VSConstants.S_OK)
            {
                return result;
            }

            if (commandGroupGuid == VSConstants.VSStd2K)
            {
                if (commandId == (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU)
                {
                    _contentWrapper.OpenContextMenu();
                    return VSConstants.S_OK;
                }
            }

            return ((IOleCommandTarget)TableControl).Exec(ref commandGroupGuid, commandId, commandExecOption, pvaIn, pvaOut);
        }

        protected override bool PreProcessMessage(ref Message m) =>
            ContentWrapper.PreProcessMessage(ref m, this) || base.PreProcessMessage(ref m);

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (disposing)
                {
                    SetTableControl(null);
                }

                base.Dispose(disposing);
            }
            finally
            {
                IsDisposed = true;
            }
        }
    }
}
