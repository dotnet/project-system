// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Threading;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    [Guid(MessageListToolWindowGuidString)]
    internal sealed class MessageListToolWindow : TableToolWindow, IVsSelectionEvents
    {
        public const string MessageListToolWindowGuidString = "75437DA3-BEEA-4CAF-8EB4-7F93FD6C46C6";

        private const string MessageTable = nameof(MessageTable);

        private int _previousSelectedItemIndex = -1;

        // _entries.Item1 == all entries from the last entries changed event;
        // _entries.Item2 == the filtered entries from the same event.
        // Save this as a tuple so that, if accessed from another thread, the tuple gives a consistent snapshot.
        private Tuple<IReadOnlyCollection<ITableEntryHandle>, IReadOnlyCollection<ITableEntryHandle>> _entries = new Tuple<IReadOnlyCollection<ITableEntryHandle>, IReadOnlyCollection<ITableEntryHandle>>(new ITableEntryHandle[0], new ITableEntryHandle[0]);

        private IReadOnlyList<ColumnState> _columnStates;

        private ITableDataSource _dataSource;

        private static readonly IReadOnlyList<string> ErrorStrings = new[] { BinaryLogEditorResources.ErrorsCategory, BinaryLogEditorResources.ErrorActiveCategory, BinaryLogEditorResources.ErrorInactiveCategory };
        private static readonly IReadOnlyList<string> WarningStrings = new[] { BinaryLogEditorResources.WarningsCategory, BinaryLogEditorResources.WarningActiveCategory, BinaryLogEditorResources.WarningInactiveCategory };
        private static readonly IReadOnlyList<string> MessageStrings = new[] { BinaryLogEditorResources.MessagesCategory, BinaryLogEditorResources.MessageActiveCategory, BinaryLogEditorResources.MessageInactiveCategory };

        private readonly IVsMonitorSelection _monitorSelection;
        private uint _eventsCookie;

        private int _entriesChangedEventCount;

        private string _errorsLabel;
        private string _warningsLabel;
        private string _messagesLabel;

        private bool AreErrorsShown
        {
            get => FilterIncludes(TableControl.GetFilter(StandardTableColumnDefinitions.ErrorSeverity), ErrorStrings);

            set => SetIsShown(ErrorStrings, value);
        }

        private bool AreWarningsShown
        {
            get => FilterIncludes(TableControl.GetFilter(StandardTableColumnDefinitions.ErrorSeverity), WarningStrings);

            set => SetIsShown(WarningStrings, value);
        }

        private bool AreMessagesShown
        {
            get => FilterIncludes(TableControl.GetFilter(StandardTableColumnDefinitions.ErrorSeverity), MessageStrings);

            set => SetIsShown(MessageStrings, value);
        }

        protected override string SearchWatermark => BinaryLogEditorResources.MessageListSearchWatermark;

        protected override string WindowCaption => BinaryLogEditorResources.MessageListWindowTitle;

        protected override int ToolbarMenuId => ProjectSystemToolsPackage.MessageListToolbarMenuId;

        protected override int ContextMenuId => -1;

        public MessageListToolWindow()
        {
            UpdateLabels(0, 0, 0, 0, 0, 0);

            var defaultColumns = new List<ColumnState>
            {
                new ColumnState2(StandardTableColumnDefinitions.DetailsExpander, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.ErrorSeverity, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.ErrorCode, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.Text, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.ProjectName, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions2.Path, isVisible: false, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.DocumentName, isVisible: true, width: 0),
                new ColumnState2(TableColumnNames.Time, isVisible: true, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.Line, isVisible: false, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.Column, isVisible: false, width: 0),
                new ColumnState2(StandardTableColumnDefinitions.ErrorCategory, isVisible: false, width: 0)
            };

            var columns = new[]
            {
                StandardTableColumnDefinitions.DetailsExpander,
                StandardTableColumnDefinitions.ErrorSeverity,
                StandardTableColumnDefinitions.Text,
                StandardTableColumnDefinitions2.Path,
                StandardTableColumnDefinitions.DocumentName,
                TableColumnNames.Time,
                StandardTableColumnDefinitions.Line,
                StandardTableColumnDefinitions.Column,
                StandardTableColumnDefinitions.ProjectName
            };

            var tableManager = ProjectSystemToolsPackage.TableManagerProvider.GetTableManager(MessageTable);
            var columnState = TableSettingLoader.LoadSettings(MessageTable, defaultColumns);
            var tableControl = (IWpfTableControl2)ProjectSystemToolsPackage.TableControlProvider.CreateControl(tableManager, true, columnState, columns);

            tableControl.RaiseDataUnstableChangeDelay = TimeSpan.Zero;
            tableControl.KeepSelectionInView = true;
            tableControl.ShowGroupingLine = false;

            SetTableControl(tableControl);

            TableSettingLoader.LoadSwitch(MessageTable, nameof(AreErrorsShown), true, out var areErrorsShown);
            TableSettingLoader.LoadSwitch(MessageTable, nameof(AreWarningsShown), true, out var areWarningsShown);
            TableSettingLoader.LoadSwitch(MessageTable, nameof(AreMessagesShown), true, out var areMessagesShown);

            AreErrorsShown = areErrorsShown;
            AreWarningsShown = areWarningsShown;
            AreMessagesShown = areMessagesShown;

            _monitorSelection = GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_eventsCookie != VSConstants.VSCOOKIE_NIL)
            {
                _monitorSelection?.UnadviseSelectionEvents(_eventsCookie);
                _eventsCookie = VSConstants.VSCOOKIE_NIL;
            }

            TableSettingLoader.SaveSettings(MessageTable, TableControl);
            TableSettingLoader.SaveSwitch(MessageTable, nameof(AreErrorsShown), AreErrorsShown);
            TableSettingLoader.SaveSwitch(MessageTable, nameof(AreWarningsShown), AreWarningsShown);
            TableSettingLoader.SaveSwitch(MessageTable, nameof(AreMessagesShown), AreMessagesShown);

            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            _monitorSelection?.AdviseSelectionEvents(this, out _eventsCookie);
        }

        protected override void SetTableControl(IWpfTableControl2 control)
        {
            if (TableControl != null)
            {
                TableControl.EntriesChanged -= OnEntriesChanged;
                TableControl.PreEntriesChanged -= OnPreEntriesChanged;
                TableControl.Manager?.RemoveSource(_dataSource);
            }

            base.SetTableControl(control);

            if (control != null)
            {
                TableControl.PreEntriesChanged += OnPreEntriesChanged;
                TableControl.EntriesChanged += OnEntriesChanged;
                TableControl.SortFunction = Compare;
                TableControl.Manager?.AddSource(_dataSource);
            }
        }

        private static bool FilterIncludes(IEntryFilter entryFilter, IReadOnlyList<string> labels) =>
            !(entryFilter is ColumnHashSetFilter filter) || labels.All(label => !filter.ExcludedContains(label));

        private void SetIsShown(IReadOnlyList<string> labels, bool value)
        {
            var filter = TableControl.GetFilter(StandardTableColumnDefinitions.ErrorSeverity) as ColumnHashSetFilter;

            var newFilter = value ? CloneAndRemove(filter, labels) : CloneAndAdd(filter, labels);

            TableControl.SetFilter(StandardTableColumnDefinitions.ErrorSeverity, newFilter);
        }

        private ColumnHashSetFilter CloneAndAdd(ColumnHashSetFilter source, IReadOnlyList<string> labels)
        {
            source = source == null
                ? new ColumnHashSetFilter(TableControl.ColumnDefinitionManager.GetColumnDefinition(StandardTableColumnDefinitions.ErrorSeverity),
                    labels)
                : labels.Aggregate(source, (current, label) => current.CloneAndAdd(label));

            return source;
        }

        private static ColumnHashSetFilter CloneAndRemove(ColumnHashSetFilter source, IReadOnlyList<string> labels)
        {
            for (var i = 0; i < labels.Count && source != null; ++i)
            {
                source = source.CloneAndRemove(labels[i]);
            }

            return source;
        }

        private void UpdateLabels(int totalErrors, int visibleErrors, int totalWarnings, int visibleWarnings, int totalMessages, int visibleMessages)
        {
            var newErrorsLabel = string.Format(CultureInfo.InvariantCulture,
                                                  totalErrors == visibleErrors ? BinaryLogEditorResources.SameLabel : BinaryLogEditorResources.DifferentLabel,
                                                  totalErrors == 1 ? BinaryLogEditorResources.ErrorLabel : BinaryLogEditorResources.ErrorsLabel, visibleErrors, totalErrors);

            var newWarningsLabel = string.Format(CultureInfo.InvariantCulture,
                                                    totalWarnings == visibleWarnings ? BinaryLogEditorResources.SameLabel : BinaryLogEditorResources.DifferentLabel,
                                                    totalWarnings == 1 ? BinaryLogEditorResources.WarningLabel : BinaryLogEditorResources.WarningsLabel, visibleWarnings, totalWarnings);

            var newMessagesLabel = string.Format(CultureInfo.InvariantCulture,
                                                    totalMessages == visibleMessages ? BinaryLogEditorResources.SameLabel : BinaryLogEditorResources.DifferentLabel,
                                                    totalMessages == 1 ? BinaryLogEditorResources.MessageLabel : BinaryLogEditorResources.MessagesLabel, visibleMessages, totalMessages);

            if (string.Equals(_errorsLabel, newErrorsLabel, StringComparison.Ordinal) &&
                string.Equals(_warningsLabel, newWarningsLabel, StringComparison.Ordinal) &&
                string.Equals(_messagesLabel, newMessagesLabel, StringComparison.Ordinal))
            {
                return;
            }

            _errorsLabel = newErrorsLabel;
            _warningsLabel = newWarningsLabel;
            _messagesLabel = newMessagesLabel;

            ProjectSystemToolsPackage.UpdateQueryStatus();
        }

        private void OnEntriesChanged(object sender, EntriesChangedEventArgs e)
        {
            if (ProjectSystemToolsPackage.IsDisposed)
            {
                return;
            }

            _entriesChangedEventCount++;
            var currentEntriesChangedEventCount = _entriesChangedEventCount;

            var pinnedSnapshots = new Dictionary<ITableEntriesSnapshot, ITableEntryHandle>();

            // Pinning snapshots on the UI thread is cheaper because they are already created and all we need to do is to increase the ref count.
            foreach (var entry in e.AllEntries)
            {
                if (!entry.TryGetSnapshot(out var snapshot, out var _) ||
                    pinnedSnapshots.ContainsKey(snapshot))
                {
                    continue;
                }
                entry.PinSnapshot();
                pinnedSnapshots.Add(snapshot, entry);
            }

            ProjectSystemToolsPackage.PackageTaskFactory.RunAsync(async delegate
            {
                await UpdateErrorCountAsync(currentEntriesChangedEventCount, pinnedSnapshots, e);
            });

            _entries = Tuple.Create(e.AllEntries, e.FilteredAndSortedEntries);

            RestorePreviousSelection(e);

            var newColumnStates = TableControl.ColumnStates;
            if (_columnStates != null && !ColumnStatesAreDifferent(_columnStates, newColumnStates))
            {
                return;
            }

            _columnStates = newColumnStates;
            ProjectSystemToolsPackage.UpdateQueryStatus();
        }

        private async Task UpdateErrorCountAsync(int currentEntriesChangedEventCount, Dictionary<ITableEntriesSnapshot, ITableEntryHandle> pinnedSnapshots, EntriesChangedEventArgs e)
        {
            await TaskScheduler.Default;

            var visibleErrors = 0;
            var visibleWarnings = 0;
            var visibleMessages = 0;

            var totalErrors = 0;
            var totalWarnings = 0;
            var totalMessages = 0;

            foreach (var entry in e.AllEntries)
            {
                if (currentEntriesChangedEventCount != _entriesChangedEventCount)
                {
                    break;
                }

                if (!entry.TryGetValue(StandardTableKeyNames.ErrorSeverity, out __VSERRORCATEGORY category))
                {
                    category = __VSERRORCATEGORY.EC_MESSAGE;
                }

                switch (category)
                {
                    case __VSERRORCATEGORY.EC_ERROR:
                        ++totalErrors;
                        break;
                    case __VSERRORCATEGORY.EC_WARNING:
                        ++totalWarnings;
                        break;
                    default:
                        ++totalMessages;
                        break;
                }
            }

            if (totalErrors + totalWarnings + totalMessages == e.FilteredAndSortedEntries.Count)
            {
                visibleErrors = totalErrors;
                visibleWarnings = totalWarnings;
                visibleMessages = totalMessages;
            }
            else
            {
                foreach (var entry in e.FilteredAndSortedEntries)
                {
                    if (currentEntriesChangedEventCount != _entriesChangedEventCount)
                    {
                        break;
                    }

                    if (!entry.TryGetValue(StandardTableKeyNames.ErrorSeverity, out __VSERRORCATEGORY category))
                    {
                        category = __VSERRORCATEGORY.EC_MESSAGE;
                    }

                    switch (category)
                    {
                        case __VSERRORCATEGORY.EC_ERROR:
                            ++visibleErrors;
                            break;
                        case __VSERRORCATEGORY.EC_WARNING:
                            ++visibleWarnings;
                            break;
                        default:
                            ++visibleMessages;
                            break;
                    }
                }
            }

            foreach (var entry in pinnedSnapshots.Values)
            {
                entry.UnpinSnapshot();
            }

            if (currentEntriesChangedEventCount == _entriesChangedEventCount)
            {
                await ProjectSystemToolsPackage.PackageTaskFactory.SwitchToMainThreadAsync();

                if (currentEntriesChangedEventCount == _entriesChangedEventCount)
                {
                    UpdateLabels(totalErrors, visibleErrors, totalWarnings, visibleWarnings, totalMessages, visibleMessages);
                }
            }
        }

        private void OnPreEntriesChanged(object sender, EventArgs e)
        {
            // Find the index of the 1st selected item (if any)
            _previousSelectedItemIndex = -1;
            var index = 0;

            // At this point, _entries still holds the data from previous update
            foreach (var entryHandle in _entries.Item2)
            {
                if (entryHandle.IsSelected)
                {
                    _previousSelectedItemIndex = index;
                    break;
                }

                ++index;
            }
        }

        private void RestorePreviousSelection(EntriesChangedEventArgs e)
        {
            // We do not attempt to select anything if either nothing was selected previously (_previousSelectedIndex = -1) or the 1st item was (_previousSelectedIndex = 0).
            if (_previousSelectedItemIndex <= 0 || e.FilteredAndSortedEntries.Count <= 0)
            {
                return;
            }

            // This selection was transferred from previous selection prior to the update
            // Something is selected so no need to do anything else.
            if (e.FilteredAndSortedEntries.Any(entryHandle => entryHandle.IsSelected))
            {
                return;
            }

            // No item was selected. Try and select the item at the previous selected index (or the item at the end of the list if
            // the list has shrunk to the point that the item no longer exists).
            var index = 0;
            ITableEntryHandle lastHandle = null;
            foreach (var entryHandle in e.FilteredAndSortedEntries)
            {
                lastHandle = entryHandle;

                // Select e.FilteredAndSortedEntries[_previousSelectedItemIndex] if it exists or the last one if none.
                if (++index > _previousSelectedItemIndex)
                {
                    break;
                }
            }

            if (lastHandle != null)
            {
                lastHandle.IsSelected = true;
            }
        }

        private static bool ColumnStatesAreDifferent(IReadOnlyList<ColumnState> oldColumnStates,
            IReadOnlyCollection<ColumnState> newColumnStates) =>
            oldColumnStates.Count != newColumnStates.Count || newColumnStates.Where((t, i) =>
                !string.Equals(oldColumnStates[i].Name, t.Name, StringComparison.OrdinalIgnoreCase)).Any();

        public int Compare(ITableEntryHandle left, ITableEntryHandle right)
        {
            left.TryGetValue(StandardTableKeyNames.ErrorSeverity, out var leftCategory, __VSERRORCATEGORY.EC_MESSAGE);
            right.TryGetValue(StandardTableKeyNames.ErrorSeverity, out var rightCategory, __VSERRORCATEGORY.EC_MESSAGE);
            var compare = (int)leftCategory - (int)rightCategory;
            if (compare != 0)
            {
                return compare;
            }

            left.TryGetValue(StandardTableKeyNames.ProjectName, out var leftString, string.Empty);
            right.TryGetValue(StandardTableKeyNames.ProjectName, out var rightString, string.Empty);

            compare = string.Compare(leftString, rightString, StringComparison.Ordinal);
            if (compare != 0)
            {
                return compare;
            }

            left.TryGetValue(StandardTableKeyNames.DocumentName, out leftString, string.Empty);
            right.TryGetValue(StandardTableKeyNames.DocumentName, out rightString, string.Empty);

            compare = string.Compare(leftString, rightString, StringComparison.OrdinalIgnoreCase);
            if (compare != 0)
            {
                return compare;
            }

            left.TryGetValue(StandardTableKeyNames.Line, out var leftInt, 0);
            right.TryGetValue(StandardTableKeyNames.Line, out var rightInt, 0);

            compare = leftInt - rightInt;
            if (compare != 0)
            {
                return compare;
            }

            left.TryGetValue(StandardTableKeyNames.Column, out leftInt, 0);
            right.TryGetValue(StandardTableKeyNames.Column, out rightInt, 0);

            return leftInt - rightInt;
        }

        private bool AreActionableFiltersPresent() =>
            (from filter in TableControl.GetAllFilters()
             let definition = ProjectSystemToolsPackage.TableControlProvider.GetFilterDefinition(filter.Item1)
             where definition != null
             where !definition.HasAttribute(EntryFilterDefinition.NonActionable) &&
                   filter.Item1 != SearchFilterKey
             select filter).Any();

        private void ClearAllActionableFilters()
        {
            foreach (var filter in TableControl.GetAllFilters())
            {
                var definition = ProjectSystemToolsPackage.TableControlProvider.GetFilterDefinition(filter.Item1);

                if (definition != null)
                {
                    // Ignoring search filter since there's no programmatic way to reset the search text.
                    if (!definition.HasAttribute(EntryFilterDefinition.NonActionable) && filter.Item1 != SearchFilterKey)
                    {
                        TableControl.SetFilter(filter.Item1, null);
                    }
                }
            }
        }

        protected override int InnerQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch (prgCmds[0].cmdID)
                {
                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowErrors:
                        {
                            prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                            if (AreErrorsShown)
                            {
                                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                            }

                            VsShellUtilities.SetOleCmdText(pCmdText, _errorsLabel);

                            return VSConstants.S_OK;
                        }

                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowWarnings:
                        {
                            prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                            if (AreWarningsShown)
                            {
                                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                            }

                            VsShellUtilities.SetOleCmdText(pCmdText, _warningsLabel);

                            return VSConstants.S_OK;
                        }

                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowMessages:
                        {
                            prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                            if (AreMessagesShown)
                            {
                                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                            }

                            VsShellUtilities.SetOleCmdText(pCmdText, _messagesLabel);

                            return VSConstants.S_OK;
                        }
                }
            }

            if (pguidCmdGroup == VSConstants.VsStd14 &&
                prgCmds[0].cmdID == (int)VSConstants.VSStd14CmdID.ErrorListClearFilters)
            {
                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_SUPPORTED;

                if (AreActionableFiltersPresent())
                {
                    prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_LATCHED;
                }

                return VSConstants.S_OK;
            }

            return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        protected override int InnerExec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch (nCmdId)
                {
                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowErrors:
                        AreErrorsShown = !AreErrorsShown;
                        return VSConstants.S_OK;

                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowWarnings:
                        AreWarningsShown = !AreWarningsShown;
                        return VSConstants.S_OK;

                    case (int)VSConstants.VSStd2KCmdID.ErrorListShowMessages:
                        AreMessagesShown = !AreMessagesShown;
                        return VSConstants.S_OK;

                }
            }

            if (pguidCmdGroup == VSConstants.VsStd14)
            {
                if (nCmdId == (int)VSConstants.VSStd14CmdID.ErrorListClearFilters)
                {
                    ClearAllActionableFilters();
                    return VSConstants.S_OK;
                }
            }

            return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int OnSelectionChanged(IVsHierarchy oldHierarchy, uint oldItemid, IVsMultiItemSelect oldItemSelect, ISelectionContainer oldSelectionContainer,
            IVsHierarchy pHierNew, uint newItemid, IVsMultiItemSelect newItemSelect, ISelectionContainer newSelectionContainer)
        {
            if (newSelectionContainer == null)
            {
                return VSConstants.S_OK;
            }

            if (_dataSource != null)
            {
                TableControl?.Manager?.RemoveSource(_dataSource);
            }

            if (newSelectionContainer.CountObjects(SelectionContainer.SELECTED, out var count) != VSConstants.S_OK ||
                count != 1)
            {
                return VSConstants.S_OK;
            }

            var objects = new object[1];

            if (newSelectionContainer.GetObjects(SelectionContainer.SELECTED, 1, objects) != VSConstants.S_OK ||
                !(objects[0] is SelectedObjectWrapper selectedObjectWrapper))
            {
                return VSConstants.S_OK;
            }

            _dataSource = selectedObjectWrapper;
            TableControl?.Manager?.AddSource(_dataSource);

            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object oldValue, object newValue) => VSConstants.S_OK;

        public int OnCmdUIContextChanged(uint cookie, int isActive) => VSConstants.S_OK;
    }
}
