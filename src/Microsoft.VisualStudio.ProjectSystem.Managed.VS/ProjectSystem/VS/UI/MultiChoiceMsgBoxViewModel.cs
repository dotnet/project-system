// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{

    internal partial class MultiChoiceMsgBoxViewModel
    {
        /// <summary>
        /// Creates the dialog where the number of buttons specified indicates how many "choices"
        /// are available. The button chosen by the user is returned in Result. The values
        /// are from the MultiChoiceMsgBoxResult enum. Note that 1 to 4 buttons are supported where
        /// button[0] is the right most button and is the default button.
        /// </summary>
        public MultiChoiceMsgBoxViewModel(string title, string errorText, string[] buttons)
        {
            if (buttons.Length < 1 || buttons.Length > 4)
            {
                throw new ArgumentException(null, nameof(buttons));
            }

            ErrorMsgText = errorText;

            DialogTitle = title;

            Button1Text = buttons[0];
            if (buttons.Length > 1)
            {
                Button2Text = buttons[1];
            }

            if (buttons.Length > 2)
            {
                Button3Text = buttons[2];
            }

            if (buttons.Length > 3)
            {
                Button4Text = buttons[3];
            }
            ButtonClickCommand = new RelayCommand((parameter) => {CloseDialog.Invoke(this, (MultiChoiceMsgBoxResult)(parameter)); });

        }
        
        public ICommand ButtonClickCommand { get; private set; }

        // Dialog needs to listen to this event to know when to close.
        public event EventHandler<MultiChoiceMsgBoxResult> CloseDialog;

        public string Button1Text { get; }
        public string Button2Text { get; }
        public string Button3Text { get; }
        public string Button4Text { get; }
        public string DialogTitle { get; }
        public string ErrorMsgText { get; }

        public Visibility Button1Visibility { get {return Button1Text == null? Visibility.Collapsed: Visibility.Visible;} }
        public Visibility Button2Visibility { get {return Button2Text == null? Visibility.Collapsed: Visibility.Visible;} }
        public Visibility Button3Visibility { get {return Button3Text == null? Visibility.Collapsed: Visibility.Visible;} }
        public Visibility Button4Visibility { get {return Button4Text == null? Visibility.Collapsed: Visibility.Visible;} }

    }
}
