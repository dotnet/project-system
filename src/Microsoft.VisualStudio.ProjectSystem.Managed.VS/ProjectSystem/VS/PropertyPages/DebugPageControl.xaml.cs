// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Interaction logic for DebugPageControl.xaml
    /// </summary>
    internal partial class DebugPageControl : PropertyPageControl
    {
        bool _customControlLayoutUpdateRequired = false;

        public DebugPageControl()
        {
            InitializeComponent();
            DataContextChanged += DebugPageControlControl_DataContextChanged;
            LayoutUpdated += DebugPageControl_LayoutUpdated;
        }

        void DebugPageControlControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null && e.OldValue is DebugPageViewModel)
            {
                DebugPageViewModel viewModel = e.OldValue as DebugPageViewModel;
                viewModel.FocusEnvironmentVariablesGridRow -= OnFocusEnvironmentVariableGridRow;
                viewModel.ClearEnvironmentVariablesGridError -= OnClearEnvironmentVariableGridError;
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue != null && e.NewValue is DebugPageViewModel)
            {
                DebugPageViewModel viewModel = e.NewValue as DebugPageViewModel;
                viewModel.FocusEnvironmentVariablesGridRow += OnFocusEnvironmentVariableGridRow;
                viewModel.ClearEnvironmentVariablesGridError += OnClearEnvironmentVariableGridError;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void OnClearEnvironmentVariableGridError(object sender, EventArgs e)
        {
            ClearGridError(dataGridEnvironmentVariables);
        }

        private async void OnFocusEnvironmentVariableGridRow(object sender, EventArgs e)
        {
            if (DataContext != null && DataContext is DebugPageViewModel)
            {
                await ThreadHelper.JoinableTaskFactory
                    .WithPriority(VsTaskRunContext.UIThreadBackgroundPriority)
                    .SwitchToMainThreadAsync();
                if ((DataContext as DebugPageViewModel).EnvironmentVariables.Count > 0)
                {
                    // get the new cell, set focus, then open for edit
                    var cell = WpfHelper.GetCell(dataGridEnvironmentVariables, (DataContext as DebugPageViewModel).EnvironmentVariables.Count - 1, 0);
                    cell.Focus();
                    dataGridEnvironmentVariables.BeginEdit();
                }
            }
        }

        private void ClearGridError(DataGrid dataGrid)
        {
            try
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                PropertyInfo cellErrorInfo = dataGrid.GetType().GetProperty("HasCellValidationError", bindingFlags);
                PropertyInfo rowErrorInfo = dataGrid.GetType().GetProperty("HasRowValidationError", bindingFlags);
                cellErrorInfo.SetValue(dataGrid, false, null);
                rowErrorInfo.SetValue(dataGrid, false, null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Called when a property changes on the view model. Used to detect when the custom UI changes so that
        /// the size of its grids can be determined
        /// </summary>
        private void ViewModel_PropertyChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DebugPageViewModel.ActiveProviderUserControl)))
            {
                _customControlLayoutUpdateRequired = true;
            }
        }

        /// <summary>
        /// Used to make sure the custom controls layout matches the rest of the dialog. Assumes the custom control has a 3 column
        /// grid just like the main dialog page. If it doesn't no layout update is done.
        /// </summary>
        private void DebugPageControl_LayoutUpdated(Object sender, EventArgs e)
        {
            if (_customControlLayoutUpdateRequired && _mainGrid != null && DataContext != null)
            {
                _customControlLayoutUpdateRequired = false;

                // Get the control that was added to the grid
                var customControl = ((DebugPageViewModel)DataContext).ActiveProviderUserControl;
                if (customControl != null)
                {
                    if (customControl.Content is Grid childGrid && childGrid.ColumnDefinitions.Count == _mainGrid.ColumnDefinitions.Count)
                    {
                        for (int i = 0; i < childGrid.ColumnDefinitions.Count; i++)
                        {
                            childGrid.ColumnDefinitions[i].Width = new GridLength(_mainGrid.ColumnDefinitions[i].ActualWidth);
                        }
                    }
                }
            }
        }
    }
}
