// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal partial class DebugPageControl : PropertyPageControl
    {
        private bool _customControlLayoutUpdateRequired;

        public DebugPageControl()
        {
            InitializeComponent();
            DataContextChanged += DebugPageControlControl_DataContextChanged;
            LayoutUpdated += DebugPageControl_LayoutUpdated;
            // This isn't a themed UI, but we want to enable high contrast mode.
            SetResourceReference(BackgroundProperty, SystemColors.ControlBrushKey);
        }

        private void DebugPageControlControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is DebugPageViewModel oldViewModel)
            {
                oldViewModel.FocusEnvironmentVariablesGridRow -= OnFocusEnvironmentVariableGridRow;
                oldViewModel.ClearEnvironmentVariablesGridError -= OnClearEnvironmentVariableGridError;
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is DebugPageViewModel newViewModel)
            {
                newViewModel.FocusEnvironmentVariablesGridRow += OnFocusEnvironmentVariableGridRow;
                newViewModel.ClearEnvironmentVariablesGridError += OnClearEnvironmentVariableGridError;
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void OnClearEnvironmentVariableGridError(object sender, EventArgs e)
        {
            ClearGridError(dataGridEnvironmentVariables);
        }

        private void OnFocusEnvironmentVariableGridRow(object sender, EventArgs e)
        {
            if (DataContext is DebugPageViewModel viewModel)
            {
#pragma warning disable RS0030 // Do not used banned APIs
                ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (viewModel.EnvironmentVariables.Count > 0)
                    {
                        // get the new cell, set focus, then open for edit
                        DataGridCell? cell = WpfHelper.GetCell(dataGridEnvironmentVariables, viewModel.EnvironmentVariables.Count - 1, 0);
                        cell?.Focus();
                        dataGridEnvironmentVariables.BeginEdit();
                    }
                }).FileAndForget(TelemetryEventName.Prefix);
#pragma warning restore RS0030 // Do not used banned APIs
            }
        }

        private static void ClearGridError(DataGrid dataGrid)
        {
            try
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                PropertyInfo cellErrorInfo = dataGrid.GetType().GetProperty("HasCellValidationError", bindingFlags);
                PropertyInfo rowErrorInfo = dataGrid.GetType().GetProperty("HasRowValidationError", bindingFlags);
                cellErrorInfo?.SetValue(dataGrid, false, null);
                rowErrorInfo?.SetValue(dataGrid, false, null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Called when a property changes on the view model. Used to detect when the custom UI changes so that
        /// the size of its grids can be determined
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        private void DebugPageControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (_customControlLayoutUpdateRequired && _mainGrid != null && DataContext is DebugPageViewModel viewModel)
            {
                _customControlLayoutUpdateRequired = false;

                // Get the control that was added to the grid
                UserControl customControl = viewModel.ActiveProviderUserControl;
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
