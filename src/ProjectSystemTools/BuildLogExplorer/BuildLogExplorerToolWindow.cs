// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.LogModel.Builder;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    [Guid(BuildLogExplorerToolWindowGuidString)]
    public sealed class BuildLogExplorerToolWindow : ToolWindowPane, IOleCommandTarget
    {
        public const string BuildLogExplorerToolWindowGuidString = "3A3D8BAD-16D8-4C83-9F0E-CA55521E0E6B";

        public const string BuildLogExplorerToolWindowCaption = "Build Log Explorer";

        private readonly BuildTreeViewControl _treeControl;
        private readonly SelectionContainer _selectionContainer;
        private readonly ObservableCollection<LogViewModel> _logs;

        public BuildLogExplorerToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLogExplorerToolWindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGuid, ProjectSystemToolsPackage.BuildLogExplorerToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            _logs = new ObservableCollection<LogViewModel>();

            _treeControl = new BuildTreeViewControl(_logs);
            _treeControl.SelectedItemChanged += SelectionChanged;

            _selectionContainer = new SelectionContainer(true, true);

            Content = _treeControl;
        }

        private void ClearLogs()
        {
            _logs.Clear();
        }

        public void AddLog(string filename)
        {
            try
            {
                var replayer = new BinaryLogReplayEventSource();
                var builder = new ModelBuilder(replayer);
                replayer.Replay(filename);
                var log = builder.Finish();
                _logs.Add(new LogViewModel(filename, log));
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aggregateException)
                {
                    _logs.Add(new LogViewModel(filename, aggregateException.InnerExceptions));
                }
                else
                {
                    _logs.Add(new LogViewModel(filename, new [] {ex}));
                }
            }
        }

        private void SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(GetService(typeof(STrackSelection)) is ITrackSelection track))
            {
                return;
            }

            var objects = new List<object>();

            if (e.NewValue is BaseViewModel viewModel)
            {
                var propertyObject = viewModel.Properties;
                if (propertyObject != null)
                {
                    objects.Add(propertyObject);
                }
            }

            _selectionContainer.SelectableObjects = objects.ToArray();
            _selectionContainer.SelectedObjects = objects.ToArray();

            track.OnSelectChange(_selectionContainer);

            if (GetService(typeof(SVsUIShell)) is IVsUIShell shell)
            {
                var propertyBrowser = new Guid(ToolWindowGuids.PropertyBrowser);
                shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref propertyBrowser, out var frame);
                frame?.ShowNoActivate();
            }
        }

        int IOleCommandTarget.QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
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
                case ProjectSystemToolsPackage.AddLogCommandId:
                    visible = true;
                    enabled = true;
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
            var handled = true;

            switch (commandId)
            {
                case ProjectSystemToolsPackage.AddLogCommandId:
                    AddLog();
                    break;

                case ProjectSystemToolsPackage.ClearCommandId:
                    ClearLogs();
                    break;

                default:
                    handled = false;
                    break;
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        private void AddLog()
        {
            var openFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".binlog",
                Filter = BuildLogExplorerResources.AddLogFilter,
                Multiselect = true,
                Title = BuildLogExplorerResources.AddLog
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            foreach (var file in openFileDialog.FileNames)
            {
                AddLog(file);
            }
        }
    }
}
