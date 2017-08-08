// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public sealed class BuildLoggingToolWindow : ToolWindowPane, IOleCommandTarget, IVsWindowSearch
    {
        public const string BuildLogging = "BuildLogging";
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";
        public const string BuildLoggingToolWindowCaption = "Build Logging";
        public const string SearchFilterKey = "BuildLogSearchFilter";

        private static readonly Guid BuildLoggingToolWindowSearchCategory = new Guid("6D3BC803-1271-4909-BA24-2921AF7F029B");

        private readonly IBuildTableDataSource _dataSource;
        private IVsSolutionBuildManager5 _updateSolutionEventsService;
        private readonly uint _updateSolutionEventsCookie;

        private readonly ContentWrapper _contentWrapper;
        private IWpfTableControl2 _tableControl;
        private bool _isDisposed;

        bool IVsWindowSearch.SearchEnabled => true;

        Guid IVsWindowSearch.Category => BuildLoggingToolWindowSearchCategory;

        IVsEnumWindowSearchFilters IVsWindowSearch.SearchFiltersEnum => null;

        IVsEnumWindowSearchOptions IVsWindowSearch.SearchOptionsEnum => null;

        public ITableManager Manager => _tableControl.Manager;

        public BuildLoggingToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLoggingToolWindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGroupGuid, ProjectSystemToolsPackage.BuildLoggingToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _dataSource = componentModel.GetService<IBuildTableDataSource>();

            _updateSolutionEventsService = ((IServiceProvider)ProjectSystemToolsPackage.Instance).GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager5;
            _updateSolutionEventsService?.AdviseUpdateSolutionEvents4(_dataSource, out _updateSolutionEventsCookie);

            _contentWrapper = new ContentWrapper(/* TODO */0x0000);
            Content = _contentWrapper;

            ResetTableControl();
        }

        private void ResetTableControl()
        {
            var columns = new[]
            {
                StandardTableColumnDefinitions.DetailsExpander,
                TableColumnNames.Build,
                StandardTableColumnDefinitions.ProjectName,
                TableColumnNames.Dimensions,
                TableColumnNames.Targets,
                TableColumnNames.Elapsed,
                TableColumnNames.Status
            };

            var columnStates = new List<ColumnState>
            {
                new ColumnState2(StandardTableColumnDefinitions.DetailsExpander, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Build, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.ProjectName, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Dimensions, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Targets, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Elapsed, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Status, isVisible: true, width: 0)
            };

            var newManager = ProjectSystemToolsPackage.TableManagerProvider.GetTableManager(BuildLogging);
            var tableControl = (IWpfTableControl2)ProjectSystemToolsPackage.TableControlProvider.CreateControl(newManager, true, columnStates, columns);

            tableControl.RaiseDataUnstableChangeDelay = TimeSpan.Zero;
            tableControl.KeepSelectionInView = false;
            tableControl.ShowGroupingLine = true;

            SetTableControl(tableControl);
        }

        protected override void Initialize()
        {
            base.Initialize();

            // When we are activated, we push our command target as the current Result List so that
            // next and previous result will come to us.We stay the current result list (even if we
            // lose activate) until some other window pushes a result list.

            var trackSelectionEx = GetService(typeof(SVsTrackSelectionEx)) as IVsTrackSelectionEx;
            System.Diagnostics.Debug.Assert(trackSelectionEx != null,
                $"Unable to get IVsTrackSelectionEx for {Caption} window");

            // Make sure the find manager is alive before sending the results list event (since the find manager listens to that event to route F8/etc.
            if (trackSelectionEx == null || ProjectSystemToolsPackage.VsFindManager == null)
            {
                return;
            }

            if (!ErrorHandler.Succeeded(trackSelectionEx.OnElementValueChange((uint)VSConstants.VSSELELEMID.SEID_ResultList, 0 /*false*/, this)))
            {
                System.Diagnostics.Debug.Fail(
                    $"Can't push {GetType().Name} as the Shell's current command target");
            }
        }

        private static void OnFiltersChanged(object sender, FiltersChangedEventArgs e) =>
            ProjectSystemToolsPackage.UpdateQueryStatus();

        private static void OnGroupingsChanged(object sender, EventArgs e) =>
            ProjectSystemToolsPackage.UpdateQueryStatus();

        private void SetTableControl(IWpfTableControl2 tableControl)
        {
            if (_tableControl != null)
            {
                _dataSource.Manager = null;
                _tableControl.FiltersChanged -= OnFiltersChanged;
                _tableControl.GroupingsChanged -= OnGroupingsChanged;
                _contentWrapper.Child = null;
                _tableControl.Dispose();
            }

            _tableControl = tableControl;

            if (tableControl != null)
            {
                _contentWrapper.Child = _tableControl.Control;

                _tableControl.FiltersChanged += OnFiltersChanged;
                _tableControl.GroupingsChanged += OnGroupingsChanged;

                _dataSource.Manager = _tableControl.Manager;
            }
        }

        int IOleCommandTarget.QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
            if (commandGroupGuid != ProjectSystemToolsPackage.CommandSetGuid)
            {
                return ((IOleCommandTarget)_tableControl).QueryStatus(ref commandGroupGuid, commandCount, commands, commandText);
            }

            if (commandCount != 1)
            {
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            var cmd = commands[0];

            var handled = true;
            var enabled = false;
            var visible = false;
            var latched = false;

            switch (cmd.cmdID)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    visible = true;
                    enabled = !_dataSource.IsLogging;
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    visible = true;
                    enabled = _dataSource.IsLogging;
                    break;

                case ProjectSystemToolsPackage.ClearCommandId:
                    visible = true;
                    enabled = true;
                    break;

                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                commands[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

                if (enabled)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                }

                if (latched)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                }

                if (!visible)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_INVISIBLE;
                }
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        int IOleCommandTarget.Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecOption, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (commandGroupGuid == VSConstants.VSStd2K)
            {
                if (commandId == (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU)
                {
                    OpenContextMenu();
                    return VSConstants.S_OK;
                }
            }

            if (commandGroupGuid != ProjectSystemToolsPackage.CommandSetGuid)
            {
                return ((IOleCommandTarget)_tableControl).Exec(ref commandGroupGuid, commandId, commandExecOption, pvaIn, pvaOut);
            }

            var handled = true;

            switch (commandId)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    _dataSource.Start();
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    _dataSource.Stop();
                    break;

                case ProjectSystemToolsPackage.ClearCommandId:
                    _dataSource.Clear();
                    break;

                default:
                    handled = false;
                    break;
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        IVsSearchTask IVsWindowSearch.CreateSearch([ComAliasName("VsShell.VSCOOKIE")]uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (_tableControl != null)
            {
                return new BuildLogSearchTask(dwCookie, pSearchQuery, pSearchCallback, _tableControl);
            }

            System.Diagnostics.Debug.Fail("Attempting to search before initializing window");
            throw new InvalidOperationException("Attempting to search before initializing window");
        }

        void IVsWindowSearch.ClearSearch()
        {
            if (_tableControl != null)
            {
                _tableControl.SetFilter(SearchFilterKey, null);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Attempting to clear before initializing ErrorListWindow");
            }
        }

        private static void SetValue(IVsUIDataSource ds, string prop, IVsUIObject value)
        {
            Validate.IsNotNull(ds, "ds");
            Validate.IsNotNullAndNotEmpty(prop, "prop");

            var result = ds.SetValue(prop, value);

            // IVsUIDataSource.SetData returns S_FALSE if the new value is the same as the old value
            if (!ErrorHandler.Succeeded(result))
            {
                throw new COMException(Resources.CannotSetProperty, result);
            }
        }

        private static void SetValue(IVsUIDataSource ds, string prop, uint value)
        {
            SetValue(ds, prop, BuiltInPropertyValue.Create(value));
        }

        private static void SetValue(IVsUIDataSource ds, string prop, bool value)
        {
            SetValue(ds, prop, BuiltInPropertyValue.Create(value));
        }

        private static void SetValue(IVsUIDataSource ds, string prop, string value)
        {
            SetValue(ds, prop, BuiltInPropertyValue.Create(value));
        }

        void IVsWindowSearch.ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, 200);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartType, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartDelay, 100);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchUseMRU, true);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.PrefixFilterMRUItems, false);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.MaximumMRUItems, 25);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchWatermark, Resources.SearchWatermark);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchPopupAutoDropdown, false);
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlBorderThickness, "1");
            SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchProgressType, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
        }

        bool IVsWindowSearch.OnNavigationKeyDown([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSEARCHNAVIGATIONKEY")]uint dwNavigationKey, [ComAliasName("VsShell.VSUIACCELMODIFIERS")]uint dwModifiers) => false;

        protected override object GetService(Type serviceType) => 
            serviceType == typeof(IWpfTableControl)
                ? _tableControl
                : (serviceType.IsEquivalentTo(typeof(IOleCommandTarget)) ? this : base.GetService(serviceType));

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                if (disposing)
                {
                    // The side effect of setting table control to null is to dispose of the existing one
                    SetTableControl(null);

                    _updateSolutionEventsService.UnadviseUpdateSolutionEvents4(_updateSolutionEventsCookie);
                    _updateSolutionEventsService = null;
                }

                base.Dispose(disposing);
            }
            finally
            {
                _isDisposed = true;
            }
        }

        internal void OpenContextMenu() => _contentWrapper.OpenContextMenu();

        protected override bool PreProcessMessage(ref Message m) => 
            ContentWrapper.PreProcessMessage(ref m, this) || base.PreProcessMessage(ref m);
    }
}
