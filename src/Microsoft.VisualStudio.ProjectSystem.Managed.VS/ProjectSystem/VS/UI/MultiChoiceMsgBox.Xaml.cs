
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows;

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
        public MultiChoiceMsgBox(string title, string errorText, string[] buttons)
        {
            if(buttons.Length < 1 || buttons.Length > 4)
            {
                throw new ArgumentException(null, nameof(buttons));
            }

            InitializeComponent();

            // Make sure the datacontext points to this class
            DataContext = this;

            WarnText.Text = errorText;

            DialogTitle = title;

            Button1Text = buttons[0];
            if(buttons.Length > 1)
            {
                Button2Text = buttons[1];
            }

            if(buttons.Length > 2)
            {
                Button3Text = buttons[2];
            }

            if(buttons.Length > 3)
            {
                Button4Text = buttons[3];
            }
        }

        public string Button1Text { get; private set; }
        public string Button2Text { get; private set; }
        public string Button3Text { get; private set; }
        public string Button4Text { get; private set; }
        public string DialogTitle { get; private set; }

        public MultiChoiceMsgBoxResult SelectedAction { get; private set; } 

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = MultiChoiceMsgBoxResult.Button4;
            DialogResult = true;
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = MultiChoiceMsgBoxResult.Button3;
            DialogResult = true;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = MultiChoiceMsgBoxResult.Button2;
            DialogResult = true;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = MultiChoiceMsgBoxResult.Button1;
            DialogResult = true;
        }
    }
}
