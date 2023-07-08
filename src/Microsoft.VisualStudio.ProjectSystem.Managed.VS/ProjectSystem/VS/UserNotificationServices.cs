// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using static Microsoft.VisualStudio.VSConstants;
using Microsoft.VisualStudio.PlatformUI;
using System.Globalization;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IUserNotificationServices))]
    internal class UserNotificationServices : IUserNotificationServices
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public UserNotificationServices(SVsServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public bool Confirm(string message)
        {
            MessageBoxResult result = ShowMessageBox(message, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, MessageBoxResult.IDYES);

            return result == MessageBoxResult.IDYES;
        }

        public void ShowWarning(string warning)
        {
            ShowMessageBox(warning, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK);
        }

        public void ShowError(string error)
        {
            ShowMessageBox(error, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK);
        }

        private MessageBoxResult ShowMessageBox(string message, OLEMSGICON icon, OLEMSGBUTTON button, MessageBoxResult defaultResult = MessageBoxResult.IDOK)
        {
            _threadingService.VerifyOnUIThread();

            if (VsShellUtilities.IsInAutomationFunction(_serviceProvider))
                return defaultResult;

            return (MessageBoxResult)VsShellUtilities.ShowMessageBox(_serviceProvider, message, null, icon, button, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public bool Confirm(string message, out bool disablePromptMessage)
        {
            string dontShowAgainMessage = string.Format(CultureInfo.CurrentCulture, VSResources.DontShowAgain);

            var userSelection = MessageDialog.Show("Microsoft Visual Studio", message, MessageDialogCommandSet.YesNo, dontShowAgainMessage,
                    out disablePromptMessage);

            return userSelection == MessageDialogCommand.Yes;
        }
    }
}
