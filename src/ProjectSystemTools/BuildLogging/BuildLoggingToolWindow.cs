// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
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
using Microsoft.Win32;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public sealed class BuildLoggingToolWindow : ToolWindowPane, IOleCommandTarget, IVsWindowSearch
    {
        public const string BuildLogging = "BuildLogging";
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";
        public const string BuildLoggingToolWindowCaption = "Build Logging";
        public const string SearchFilterKey = "BuildLogSearchFilter";

        private const string ColumnWidth = "Width";
        private const string ColumnVisibility = "Visibility";
        private const string ColumnSortPriority = "SortPriority";
        private const string ColumnSortDown = "DescendingSort";
        private const string ColumnOrder = "ColumnOrder";
        private const string ColumnGroupingPriority = "GroupingPriority";
        private const string ColumnsKey = "ProjectSystemTools\\BuildLogging\\Columns";

        private static readonly Guid BuildLoggingToolWindowSearchCategory = new Guid("6D3BC803-1271-4909-BA24-2921AF7F029B");

        private readonly IBuildTableDataSource _dataSource;

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

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGuid, ProjectSystemToolsPackage.BuildLoggingToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _dataSource = componentModel.GetService<IBuildTableDataSource>();

            _contentWrapper = new ContentWrapper(ProjectSystemToolsPackage.BuildLoggingContextMenuId);
            Content = _contentWrapper;

            ResetTableControl();
        }

        public static IEnumerable<ColumnState> LoadSettings()
        {
            var columns = new List<Tuple<int, ColumnState>>();

            using (var rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.Instance, __VsLocalRegistryType.RegType_UserSettings, writable: false))
            {
                using (var columnsSubKey = rootKey.OpenSubKey(ColumnsKey, writable: false))
                {
                    if (columnsSubKey == null)
                    {
                        var defaultColumns = new List<ColumnState>
                        {
                            new ColumnState2(StandardTableColumnDefinitions.DetailsExpander, isVisible: true, width: 25),
                            new ColumnState2(StandardTableColumnDefinitions.ProjectName, isVisible: true, width: 250),
                            new ColumnState2(TableColumnNames.Dimensions, isVisible: true, width: 100),
                            new ColumnState2(TableColumnNames.Targets, isVisible: true, width: 700),
                            new ColumnState2(TableColumnNames.DesignTime, isVisible: true, width: 100),
                            new ColumnState2(TableColumnNames.StartTime, isVisible: true, width: 125),
                            new ColumnState2(TableColumnNames.Elapsed, isVisible: true, width: 100),
                            new ColumnState2(TableColumnNames.Status, isVisible: true, width: 100)
                        };

                        return defaultColumns;
                    }

                    foreach (var columnName in columnsSubKey.GetSubKeyNames())
                    {
                        using (var columnSubKey = columnsSubKey.OpenSubKey(columnName, writable: false))
                        {
                            if (columnSubKey == null)
                            {
                                continue;
                            }

                            var descendingSort = (int)columnSubKey.GetValue(ColumnSortDown, 1) != 0;
                            var sortPriority = (int)columnSubKey.GetValue(ColumnSortPriority, 0);

                            var groupingPriority = (int)columnSubKey.GetValue(ColumnGroupingPriority, 0);

                            var columnOrder = (int)columnSubKey.GetValue(ColumnOrder, int.MaxValue);
                            var visibility = (int)columnSubKey.GetValue(ColumnVisibility, 0) != 0;
                            var width = (int)columnSubKey.GetValue(ColumnWidth, 20);

                            var state = new ColumnState2(columnName, visibility, width, sortPriority, descendingSort, groupingPriority);

                            columns.Add(new Tuple<int, ColumnState>(columnOrder, state));
                        }
                    }
                }
            }

            columns.Sort((a, b) => a.Item1 - b.Item1);

            return columns.Select(a => a.Item2);
        }

        private void ResetTableControl()
        {
            var columns = new[]
            {
                StandardTableColumnDefinitions.DetailsExpander,
                StandardTableColumnDefinitions.ProjectName,
                TableColumnNames.Dimensions,
                TableColumnNames.Targets,
                TableColumnNames.DesignTime,
                TableColumnNames.StartTime,
                TableColumnNames.Elapsed,
                TableColumnNames.Status
            };

            var newManager = ProjectSystemToolsPackage.TableManagerProvider.GetTableManager(BuildLogging);
            var columnStates = LoadSettings();
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

        protected override void OnClose()
        {
            var columns = _tableControl.ColumnStates;

            using (var rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.Instance, __VsLocalRegistryType.RegType_UserSettings, writable: true))
            {
                using (var columnsSubKey = rootKey.CreateSubKey(ColumnsKey))
                {
                    if (columnsSubKey == null)
                    {
                        return;
                    }

                    for (var i = 0; i < columns.Count; i++)
                    {
                        var column = columns[i];

                        using (var columnSubKey = columnsSubKey.CreateSubKey(column.Name))
                        {
                            if (columnSubKey != null)
                            {
                                columnSubKey.SetValue(ColumnOrder, i, RegistryValueKind.DWord);
                                columnSubKey.SetValue(ColumnVisibility, column.IsVisible ? 1 : 0, RegistryValueKind.DWord);
                                columnSubKey.SetValue(ColumnWidth, (int)(column.Width), RegistryValueKind.DWord);

                                columnSubKey.SetValue(ColumnSortDown, column.DescendingSort ? 1 : 0, RegistryValueKind.DWord);
                                columnSubKey.SetValue(ColumnSortPriority, column.SortPriority, RegistryValueKind.DWord);

                                if (column is ColumnState2 cs2)
                                {
                                    columnSubKey.SetValue(ColumnGroupingPriority, cs2.GroupingPriority, RegistryValueKind.DWord);
                                }
                            }
                        }
                    }
                }
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

        private void SaveLogs()
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog()
            {
                Description = Resources.LogFolderDescription
            };

            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            foreach (var entry in _tableControl.SelectedEntries)
            {
                if (!entry.TryGetValue(TableKeyNames.LogPath, out string logPath) ||
                    !entry.TryGetValue(TableKeyNames.Filename, out string filename))
                {
                    continue;
                }
                try
                {
                    File.Copy(logPath, Path.Combine(folderBrowser.SelectedPath, filename));
                }
                catch
                {
                    // Oh, well...
                }
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
                case ProjectSystemToolsPackage.SaveLogsCommandId:
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
                    _contentWrapper.OpenContextMenu();
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

                case ProjectSystemToolsPackage.SaveLogsCommandId:
                    SaveLogs();
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
                }

                base.Dispose(disposing);
            }
            finally
            {
                _isDisposed = true;
            }
        }

        protected override bool PreProcessMessage(ref Message m) =>
            ContentWrapper.PreProcessMessage(ref m, this) || base.PreProcessMessage(ref m);
    }
}
