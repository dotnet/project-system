using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.LogModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    internal sealed class BinaryLogEditorPane : WindowPane, IOleCommandTarget
    {
        private readonly BuildTreeViewControl _treeControl;
        private readonly SelectionContainer _selectionContainer;
        private readonly ObservableCollection<BaseViewModel> _items;
        private readonly BinaryLogDocumentData _documentData;

        public BinaryLogEditorPane(BinaryLogDocumentData documentData)
        {
            _selectionContainer = new SelectionContainer(true, true);
            _items = new ObservableCollection<BaseViewModel>();
            _documentData = documentData;
            _documentData.Loaded += (sender, args) =>
            {
                if (_documentData.Log == null)
                {
                    _items.Add(new ListViewModel<Exception>("Exceptions", _documentData.Exceptions, ex => new ExceptionViewModel(ex)));
                }
                else
                {
                    if (_documentData.Log.Evaluations.Any())
                    {
                        _items.Add(new ListViewModel<Evaluation>(
                            $"Evaluations ({_documentData.Log.Evaluations.SelectMany(e => e.EvaluatedProjects).Aggregate(TimeSpan.Zero, (t, p) => t + (p.EndTime - p.StartTime)):mm':'ss'.'ffff})",
                            _documentData.Log.Evaluations,
                            e => e.EvaluatedProjects.Count == 1
                                ? (BaseViewModel) new EvaluatedProjectViewModel(e)
                                : new EvaluationViewModel(e)));
                    }

                    if (_documentData.Log.Build?.Project != null)
                    {
                        _items.Add(new BuildViewModel(_documentData.Log.Build));
                    }

                }
            };
            _treeControl = new BuildTreeViewControl(_items);
            _treeControl.SelectedItemChanged += SelectionChanged;
            Content = _treeControl;
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
    }
}