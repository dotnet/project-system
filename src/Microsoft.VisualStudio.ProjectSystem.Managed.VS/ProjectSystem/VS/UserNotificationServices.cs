// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using static Microsoft.VisualStudio.VSConstants;

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
    }
}
