// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.LogModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using Task = Microsoft.VisualStudio.ProjectSystem.LogModel.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    internal sealed class BinaryLogEditorPane : WindowPane, IOleCommandTarget
    {
        private readonly SelectionContainer _selectionContainer;
        private readonly ObservableCollection<BaseViewModel> _buildTreeViewItems = new ObservableCollection<BaseViewModel>();
        private readonly ObservableCollection<BaseViewModel> _evaluationTreeViewItems = new ObservableCollection<BaseViewModel>();
        private readonly ObservableCollection<TargetListViewModel> _targetListViewItems = new ObservableCollection<TargetListViewModel>();
        private readonly ObservableCollection<TaskListViewModel> _taskListViewItems = new ObservableCollection<TaskListViewModel>();
        private readonly ObservableCollection<EvaluationListViewModel> _evaluationListViewItems = new ObservableCollection<EvaluationListViewModel>();
        private readonly BinaryLogDocumentData _documentData;

        public BinaryLogEditorPane(BinaryLogDocumentData documentData)
        {
            _selectionContainer = new SelectionContainer(true, true);
            _documentData = documentData;
            _documentData.Loaded += OnDocumentDataLoaded;
            var logControl = new LogViewControl();
            logControl.AddHandler(TreeView.SelectedItemChangedEvent, (RoutedPropertyChangedEventHandler<object>)SelectionChanged);
            logControl.AddHandler(DataGrid.SelectionChangedEvent, (SelectionChangedEventHandler)GridSelectionChanged);
            logControl.BuildTreeTab.DataContext = new RootViewModel(_buildTreeViewItems);
            logControl.EvaluationTreeTab.DataContext = new RootViewModel(_evaluationTreeViewItems);
            logControl.TargetsTab.DataContext = _targetListViewItems;
            logControl.TasksTab.DataContext = _taskListViewItems;
            logControl.EvaluationTab.DataContext = _evaluationListViewItems;
            Content = logControl;
        }

        private static IEnumerable<Target> CollectTargets(Project project) => 
            project.Targets.Union(project.Targets.SelectMany(t => t.Tasks).SelectMany(t => t.ChildProjects).SelectMany(CollectTargets));

        private static IEnumerable<Task> CollectTasks(Project project) =>
            project.Targets.SelectMany(target => target.Tasks).Union(project.Targets.SelectMany(t => t.Tasks).SelectMany(t => t.ChildProjects).SelectMany(CollectTasks));

        private static IEnumerable<EvaluatedLocation> CollectEvaluations(IEnumerable<EvaluatedLocation> locations) =>
            !locations.Any() ? locations : locations.Union(CollectEvaluations(locations.SelectMany(l => l.Children)));

        private void OnDocumentDataLoaded(object sender, EventArgs args)
        {
            if (_documentData.Log.Exceptions.Any())
            {
                _buildTreeViewItems.Add(new ListViewModel<Exception>("Exceptions", _documentData.Log.Exceptions, ex => new ExceptionViewModel(ex)));
            }

            if (_documentData.Log.Evaluations.Any())
            {
                _evaluationTreeViewItems.Add(new ListViewModel<Evaluation>($"Evaluations ({_documentData.Log.Evaluations.SelectMany(e => e.EvaluatedProjects).Aggregate(TimeSpan.Zero, (t, p) => t + (p.EndTime - p.StartTime)):mm':'ss'.'ffff})", _documentData.Log.Evaluations, e => e.EvaluatedProjects.Count == 1
                    ? (BaseViewModel) new EvaluatedProjectViewModel(e)
                    : new EvaluationViewModel(e)));
            }

            if (_documentData.Log.Build?.Project != null)
            {
                _buildTreeViewItems.Add(new BuildViewModel(_documentData.Log.Build));

                var allTargets = CollectTargets(_documentData.Log.Build.Project);
                var groupedTargets = allTargets.GroupBy(target => Tuple.Create(target.Name, target.SourceFilePath));
                var totalTime = _documentData.Log.Build.EndTime - _documentData.Log.Build.StartTime;
                foreach (var groupedTarget in groupedTargets)
                {
                    var time = groupedTarget.Aggregate(TimeSpan.Zero, (current, target) => current + (target.EndTime - target.StartTime));
                    _targetListViewItems.Add(new TargetListViewModel(groupedTarget.Key.Item1, groupedTarget.Key.Item2, groupedTarget.Count(), time, time.Ticks / (double)totalTime.Ticks));
                }

                var allTasks = CollectTasks(_documentData.Log.Build.Project);
                var groupedTasks = allTasks.GroupBy(task => Tuple.Create(task.Name, task.SourceFilePath));
                foreach (var groupedTask in groupedTasks)
                {
                    var time = groupedTask.Aggregate(TimeSpan.Zero, (current, task) => current + (task.EndTime - task.StartTime));
                    _taskListViewItems.Add(new TaskListViewModel(groupedTask.Key.Item1, groupedTask.Key.Item2, groupedTask.Count(), time, time.Ticks / (double)totalTime.Ticks));
                }

                var allEvaluations = CollectEvaluations(_documentData.Log.Evaluations
                    .SelectMany(e => e.EvaluatedProjects)
                    .Select(p => p.EvaluationProfile)
                    .Where(p => p != null)
                    .SelectMany(p => p.Passes)
                    .SelectMany(p => p.Locations)).ToList();
                var totalEvaluationTime = allEvaluations.Aggregate(TimeSpan.Zero, (current, e) => current + e.Time.ExclusiveTime);
                var groupedEvaluations =
                    allEvaluations.GroupBy(e => Tuple.Create(e.ElementName, e.Kind, e.File, e.Line));
                foreach (var groupedEvaluation in groupedEvaluations)
                {
                    var time = groupedEvaluation.Aggregate(TimeSpan.Zero, (current, e) => current + e.Time.ExclusiveTime);
                    _evaluationListViewItems.Add(new EvaluationListViewModel(groupedEvaluation.Key.Item1, groupedEvaluation.First().ElementDescription, groupedEvaluation.Key.Item2.ToString(), groupedEvaluation.Key.Item3, groupedEvaluation.Key.Item4, groupedEvaluation.Count(), time, time.Ticks / (double)totalEvaluationTime.Ticks));
                }
            }
        }

        int IOleCommandTarget.Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecOption, IntPtr pvaIn, IntPtr pvaOut)
            => (int)Constants.OLECMDERR_E_NOTSUPPORTED;

        int IOleCommandTarget.QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
            => (int)Constants.OLECMDERR_E_NOTSUPPORTED;

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

        private void GridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(GetService(typeof(STrackSelection)) is ITrackSelection track))
            {
                return;
            }

            var objects = new List<object>();

            if (e.AddedItems.Count != 1)
            {
                return;
            }

            if (e.AddedItems[0] is IViewModelWithProperties viewModel)
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
    }
}