// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
            _threadingService.VerifyOnUIThread();
            if (!VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                int result = VsShellUtilities.ShowMessageBox(_serviceProvider, message, null, OLEMSGICON.OLEMSGICON_QUERY,
                             OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                if (result == (int)VSConstants.MessageBoxResult.IDNO)
                {
                    return false;
                }
            }
            return true;
        }

        public void ShowWarning(string warning)
        {
            _threadingService.VerifyOnUIThread();
            if (!VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                int result = VsShellUtilities.ShowMessageBox(_serviceProvider, warning, null, OLEMSGICON.OLEMSGICON_WARNING,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public void ShowError(string error)
        {
            _threadingService.VerifyOnUIThread();
            if (!VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                int result = VsShellUtilities.ShowMessageBox(_serviceProvider, error, null, OLEMSGICON.OLEMSGICON_CRITICAL,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
