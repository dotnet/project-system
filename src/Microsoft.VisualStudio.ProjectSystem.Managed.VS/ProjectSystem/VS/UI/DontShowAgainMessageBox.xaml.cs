using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal partial class DontShowAgainMessageBox : PlatformUI.DialogWindow
    {

        public DontShowAgainMessageBox(string caption, string message, string checkboxText, bool initialStateOfCheckbox,
                                       string learnMoreText, string learnMoreUrl, IUserNotificationServices userNotificationServices)
        {
            _userNotificationServices = userNotificationServices;

            InitializeComponent();

            DataContext = this;
            DialogCaption = caption;
            MessageText = message;
            PreviewKeyDown += new KeyEventHandler(CloseOnESCkey);

            DontShowAgainCheckBox.Visibility = Visibility.Collapsed;
            if (checkboxText != null)
            {
                DontShowAgainCheckBox.Visibility = Visibility.Visible;
                CheckboxText = checkboxText;
                CheckboxState = initialStateOfCheckbox;
            }

            LearnMore.Visibility = Visibility.Collapsed;
            if (learnMoreText != null)
            {
                LearnMore.Visibility = Visibility.Visible;
                LearnMoreText = learnMoreText;
                LearnMoreCommand = new RelayCommand((parameter) =>
                {
                    try
                    {
                        Process.Start(learnMoreUrl);
                    }
                    catch (Exception ex)
                    {
                        _userNotificationServices.ShowMessageBox(ex.Message, null, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                });
            }
        }

        private IUserNotificationServices _userNotificationServices;

        //Strictly used for databinding, no notifications
        public string MessageText { get; }
        public string DialogCaption { get; }
        public string CheckboxText { get; }
        public static string OkButtonText => VSResources.OKButtonText;
        public string LearnMoreText { get; private set; }

        public ICommand LearnMoreCommand { get; set; }

        // No notifications required here either
        public bool CheckboxState { get; set; }

        // Need to hook keyboard event and force it to close on ESC keypress
        private void CloseOnESCkey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
