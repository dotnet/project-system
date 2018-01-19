// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio.ProjectSystem.LogModel.Builder;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    [Guid(BuildLogExplorerToolWindowGuidString)]
    public sealed class BuildLogExplorerToolWindow : ToolWindowPane
    {
        public const string BuildLogExplorerToolWindowGuidString = "3A3D8BAD-16D8-4C83-9F0E-CA55521E0E6B";

        public const string BuildLogExplorerToolWindowCaption = "Build Log Explorer";

        private readonly BuildTreeViewControl _treeControl;
        private readonly SelectionContainer _selectionContainer;
        private readonly ObservableCollection<LogViewModel> _logs;

        public BuildLogExplorerToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLogExplorerToolWindowCaption;

            _logs = new ObservableCollection<LogViewModel>();

            _treeControl = new BuildTreeViewControl(_logs);
            _treeControl.SelectedItemChanged += SelectionChanged;

            _selectionContainer = new SelectionContainer(true, true);

            Content = _treeControl;
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
        }
    }
}
