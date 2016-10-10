// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

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
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public bool Confirm(string message)
        {
            _threadingService.VerifyOnUIThread();
            if (!VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                var result = VsShellUtilities.ShowMessageBox(_serviceProvider, message, null, OLEMSGICON.OLEMSGICON_QUERY,
                             OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                if (result == (int)VSConstants.MessageBoxResult.IDNO)
                {
                    return false;
                }
            }
            return true;
        }

        public void NotifyFailure(string failureMessage)
        {
            _threadingService.VerifyOnUIThread();
            if (!VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                var result = VsShellUtilities.ShowMessageBox(_serviceProvider, failureMessage, null, OLEMSGICON.OLEMSGICON_WARNING,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Uses IVsUIShell to display the error associated with the hr. Will look for an error string on the current thread that was
        /// set by SetErrorInfo() and use that. Otherwise, tries to get the best error from the hResult.
        /// </summary>
        public void ReportErrorInfo(int hr)
        {
            _threadingService.VerifyOnUIThread();

            var vsUIShell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();

            var result = vsUIShell.ReportErrorInfo(hr);
        }
    }
}
