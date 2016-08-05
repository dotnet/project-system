// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{

    internal partial class MultiChoiceMsgBox : DialogWindow
    {
        /// <summary>
        /// Creates the dialog where the number of buttons specified indicates how many "choices"
        /// are available. The button chosen by the user is returned in Result. The values
        /// are from the MultiChoiceMsgBoxResult enum. Note that 1 to 4 buttons are supported where
        /// button[0] is the right most button and is the default button.
        /// </summary>
        public MultiChoiceMsgBox(string dialogTitle, string errorText, string[] buttons)
        {
            var viewModel = new MultiChoiceMsgBoxViewModel(dialogTitle, errorText, buttons);

            InitializeComponent();

            // Make sure the datacontext points to the view model
            DataContext = viewModel;

            viewModel.CloseDialog += ViewModel_CloseDialog;
        }

        /// <summary>
        /// Invoked by the view model with the dialog result
        /// </summary>
        private void ViewModel_CloseDialog(object sender, MultiChoiceMsgBoxResult e)
        {
            SelectedAction = e;
            DialogResult = true;
        }

        public MultiChoiceMsgBoxResult SelectedAction { get; private set; } 
    }
}
