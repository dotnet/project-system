// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Guid(BuildLoggingToolWindowGuidString)]
    internal sealed class BuildLoggingToolWindow : TableToolWindow
    {
        public const string BuildLogging = "BuildLogging";
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";

        private readonly IBuildTableDataSource _dataSource;
        private readonly IVsUIShellOpenDocument _openDocument;

        private BuildType _filterType = BuildType.All;

        protected override string SearchWatermark => BuildLoggingResources.SearchWatermark;

        protected override string WindowCaption => BuildLoggingResources.BuildLogWindowTitle;

        protected override int ToolbarMenuId => ProjectSystemToolsPackage.BuildLoggingToolbarMenuId;

        protected override int ContextMenuId => ProjectSystemToolsPackage.BuildLoggingContextMenuId;

        public BuildLoggingToolWindow()
        {
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _dataSource = componentModel.GetService<IBuildTableDataSource>();

            _openDocument = (IVsUIShellOpenDocument)GetService(typeof(SVsUIShellOpenDocument));

            ResetTableControl();
        }

        private void ResetTableControl()
        {
            var defaultColumns = new List<ColumnState>
            {
                new ColumnState2(StandardTableColumnDefinitions.DetailsExpander, isVisible: true, width: 25),
                new ColumnState2(StandardTableColumnDefinitions.ProjectName, isVisible: true, width: 200),
                new ColumnState2(TableColumnNames.ProjectType, isVisible: true, width: 50),
                new ColumnState2(TableColumnNames.Dimensions, isVisible: true, width: 100),
                new ColumnState2(TableColumnNames.Targets, isVisible: true, width: 700),
                new ColumnState2(TableColumnNames.BuildType, isVisible: true, width: 100),
                new ColumnState2(TableColumnNames.StartTime, isVisible: true, width: 125),
                new ColumnState2(TableColumnNames.Elapsed, isVisible: true, width: 100),
                new ColumnState2(TableColumnNames.Status, isVisible: true, width: 100)
            };

            var columns = new[]
            {
                StandardTableColumnDefinitions.DetailsExpander,
                StandardTableColumnDefinitions.ProjectName,
                TableColumnNames.ProjectType,
                TableColumnNames.Dimensions,
                TableColumnNames.Targets,
                TableColumnNames.BuildType,
                TableColumnNames.StartTime,
                TableColumnNames.Elapsed,
                TableColumnNames.Status
            };

            var newManager = ProjectSystemToolsPackage.TableManagerProvider.GetTableManager(BuildLogging);
            var columnStates = TableSettingLoader.LoadSettings(BuildLogging, defaultColumns);
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

        private void OnFiltersChanged(object sender, FiltersChangedEventArgs e)
        {
            if (e.Key == TableColumnNames.BuildType && e.NewFilter is ColumnHashSetFilter filter)
            {
                switch (filter.ExcludedCount)
                {
                    case 0:
                        _filterType = BuildType.All;
                        break;
                    default:
                        if (!filter.ExcludedContains(nameof(BuildType.Build)))
                        {
                            _filterType = BuildType.Build;
                        }
                        else if (!filter.ExcludedContains(nameof(BuildType.DesignTimeBuild)))
                        {
                            _filterType = BuildType.DesignTimeBuild;
                        }
                        else if (!filter.ExcludedContains(nameof(BuildType.Evaluation)))
                        {
                            _filterType = BuildType.Evaluation;
                        }
                        else if (!filter.ExcludedContains(nameof(BuildType.Roslyn)))
                        {
                            _filterType = BuildType.Roslyn;
                        }
                        break;
                }
            }

            ProjectSystemToolsPackage.UpdateQueryStatus();
        }

        private static void OnGroupingsChanged(object sender, EventArgs e) =>
            ProjectSystemToolsPackage.UpdateQueryStatus();

        protected override void SetTableControl(IWpfTableControl2 tableControl)
        {
            if (TableControl != null)
            {
                TableControl.Manager.RemoveSource(_dataSource);
                TableControl.FiltersChanged -= OnFiltersChanged;
                TableControl.GroupingsChanged -= OnGroupingsChanged;
            }

            base.SetTableControl(tableControl);

            if (TableControl != null)
            {
                TableControl.FiltersChanged += OnFiltersChanged;
                TableControl.GroupingsChanged += OnGroupingsChanged;

                TableControl.Manager.AddSource(_dataSource);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            TableSettingLoader.SaveSettings(BuildLogging, TableControl);
        }

        private void SaveLogs()
        {
            var folderBrowser = new FolderBrowserDialog()
            {
                Description = BuildLoggingResources.LogFolderDescription
            };

            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            foreach (var entry in TableControl.SelectedEntries)
            {
                if (!entry.TryGetValue(TableKeyNames.LogPath, out string logPath))
                {
                    continue;
                }

                var filename = Path.GetFileName(logPath);

                if (filename == null)
                {
                    continue;
                }

                try
                {
                    File.Copy(logPath, Path.Combine(folderBrowser.SelectedPath, filename));
                }
                catch (Exception e)
                {
                    var title = $"Error saving {filename}";
                    ShowExceptionMessageDialog(e, title);
                }
            }
        }

        private void OpenLogs()
        {
            foreach (var entry in TableControl.SelectedEntries)
            {
                OpenLog(entry);
            }
        }

        public void OpenLog(ITableEntryHandle tableEntry)
        {
            if (!tableEntry.TryGetValue(TableKeyNames.LogPath, out string logPath))
            {
                return;
            }

            var guid = VSConstants.LOGVIEWID_Primary;
            _openDocument.OpenDocumentViaProject(logPath, ref guid, out _, out _, out _, out var frame);
            frame?.Show();
        }

        private static void ShowExceptionMessageDialog(Exception e, string title)
        {
            var message = $@"{e.GetType().FullName}

{e.Message}

{e.StackTrace}";

            MessageDialog.Show(title, message, MessageDialogCommandSet.Ok);
        }

        protected override int InnerQueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
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
                case ProjectSystemToolsPackage.OpenLogsCommandId:
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

        private string[] GetBuildFilterComboItems() =>
            (_dataSource as BuildTableDataSource)?.SupportRoslynLogging ?? false
                ? new[]
                {
                    BuildLoggingResources.FilterBuildAll, BuildLoggingResources.FilterBuildEvaluations,
                    BuildLoggingResources.FilterBuildDesignTimeBuilds, BuildLoggingResources.FilterBuildBuilds,
                    BuildLoggingResources.FilterBuildRoslyn
                }
                : new[]
                {
                    BuildLoggingResources.FilterBuildAll, BuildLoggingResources.FilterBuildEvaluations,
                    BuildLoggingResources.FilterBuildDesignTimeBuilds, BuildLoggingResources.FilterBuildBuilds
                };

        private IEnumerable<string> GetExcluded(string include) => Enum.GetNames(typeof(BuildType))
            .Where(name => name != nameof(BuildType.None) && name != nameof(BuildType.All))
            .Where(name => name != include);

        protected override int InnerExec(ref Guid commandGroupGuid, uint commandId, uint commandExecOption, IntPtr pvaIn, IntPtr pvaOut)
        {
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

                case ProjectSystemToolsPackage.OpenLogsCommandId:
                    OpenLogs();
                    break;

                case ProjectSystemToolsPackage.BuildTypeComboCommandId:
                    var selectedType = string.Empty;
                    if (pvaOut != IntPtr.Zero)
                    {
                        switch (_filterType)
                        {
                            case BuildType.All:
                                selectedType = BuildLoggingResources.FilterBuildAll;
                                break;
                            case BuildType.Evaluation:
                                selectedType = BuildLoggingResources.FilterBuildEvaluations;
                                break;
                            case BuildType.DesignTimeBuild:
                                selectedType = BuildLoggingResources.FilterBuildDesignTimeBuilds;
                                break;
                            case BuildType.Build:
                                selectedType = BuildLoggingResources.FilterBuildBuilds;
                                break;
                            case BuildType.Roslyn:
                                selectedType = BuildLoggingResources.FilterBuildRoslyn;
                                break;
                        }

                        Marshal.GetNativeVariantForObject(selectedType, pvaOut);
                    }
                    else
                    {
                        var selectedItem = Marshal.GetObjectForNativeVariant(pvaIn);

                        selectedType = selectedItem.ToString();
                        var column = TableControl.ColumnDefinitionManager.GetColumnDefinition(TableColumnNames.BuildType);
                        if (selectedType.Equals(BuildLoggingResources.FilterBuildAll))
                        {
                            TableControl.SetFilter(TableColumnNames.BuildType, new ColumnHashSetFilter(column));
                        }
                        else if (selectedType.Equals(BuildLoggingResources.FilterBuildEvaluations))
                        {
                            TableControl.SetFilter(TableColumnNames.BuildType, new ColumnHashSetFilter(column, GetExcluded(nameof(BuildType.Evaluation))));
                        }
                        else if (selectedType.Equals(BuildLoggingResources.FilterBuildDesignTimeBuilds))
                        {
                            TableControl.SetFilter(TableColumnNames.BuildType, new ColumnHashSetFilter(column, GetExcluded(nameof(BuildType.DesignTimeBuild))));
                        }
                        else if (selectedType.Equals(BuildLoggingResources.FilterBuildBuilds))
                        {
                            TableControl.SetFilter(TableColumnNames.BuildType, new ColumnHashSetFilter(column, GetExcluded(nameof(BuildType.Build))));
                        }
                        else if (selectedType.Equals(BuildLoggingResources.FilterBuildRoslyn))
                        {
                            TableControl.SetFilter(TableColumnNames.BuildType, new ColumnHashSetFilter(column, GetExcluded(nameof(BuildType.Roslyn))));
                        }
                    }

                    break;

                case ProjectSystemToolsPackage.BuildTypeComboGetListCommandId:
                    var outParam = pvaOut;
                    Marshal.GetNativeVariantForObject(GetBuildFilterComboItems(), outParam);
                    break;

                default:
                    handled = false;
                    break;
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}
