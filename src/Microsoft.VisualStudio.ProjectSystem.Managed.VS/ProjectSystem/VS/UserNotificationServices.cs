// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IUserNotificationServices"/> that delegates onto 
    /// </summary>
    [Export(typeof(IUserNotificationServices))]
    internal class UserNotificationServices : IUserNotificationServices
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;
        private readonly IOptionsSettings _optionsSettings;

        [ImportingConstructor]
        public UserNotificationServices(IOptionsSettings optionsSettings, SVsServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(optionsSettings, nameof(optionsSettings));
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _optionsSettings = optionsSettings;
        }

        public async Task<bool> CheckPromptAsync(string promptMessage)
        {
            await _threadingService.SwitchToUIThread();

            var result = VsShellUtilities.ShowMessageBox(_serviceProvider, promptMessage, null, OLEMSGICON.OLEMSGICON_QUERY,
                          OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            if (result == (int)VSConstants.MessageBoxResult.IDNO)
            {
                return false;
            }
            return true;
        }

        public async void NotifyFailureAsync(string failureMessage)
        {
            await _threadingService.SwitchToUIThread();
            var result = VsShellUtilities.ShowMessageBox(_serviceProvider, failureMessage, null, OLEMSGICON.OLEMSGICON_WARNING,
                                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public async Task<bool> CheckPromptForRenameAsync(string oldName)
        {
            var userSetting = await _optionsSettings.GetPropertiesValueAsync("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false).ConfigureAwait(false);
            if (userSetting)
            {
                string promptMessage = string.Format(Resources.RenameSymbolPrompt, oldName);
                return await CheckPromptAsync(promptMessage).ConfigureAwait(false);
            }
            return false;
        }

        public void NotifyRenameFailureAsync(string oldName)
        {
            string failureMessage = string.Format(Resources.RenameSymbolFailed, oldName);
            NotifyFailureAsync(failureMessage);
        }

    }
}
