// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using RoslynRenamer = Microsoft.CodeAnalysis.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsEnvironmentServices"/> that delegates onto 
    /// </summary>
    [Export(typeof(IVsEnvironmentServices))]
    internal class VsEnvironmentServices : IVsEnvironmentServices
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public VsEnvironmentServices(SVsServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
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

        public async Task<T> GetEnvironmentSettingAsync<T>(string category, string page, string property, T defaultValue)
        {
            await _threadingService.SwitchToUIThread();

            EnvDTE.DTE dte = _serviceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
            var props = dte.Properties[category, page];
            if (props != null)
            {
                return ((T)props.Item(property).Value);
            }
            return defaultValue;
        }

        public async Task<bool> CheckPromptForRenameAsync(string oldName)
        {
            var userSetting = await GetEnvironmentSettingAsync<bool>("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false).ConfigureAwait(false);
            if (userSetting)
            {
                string promptMessage = string.Format(Resources.RenameSymbolPrompt, oldName);
                return await CheckPromptAsync(promptMessage).ConfigureAwait(false);
            }
            return false;
        }

        public async Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName)
        {
            var optionSet = solution.Workspace.Options;
            return await RoslynRenamer.Renamer.RenameSymbolAsync(solution, symbol, newName, optionSet).ConfigureAwait(false);
        }

        public async Task<bool> ApplyChangesToSolutionAsync(Workspace ws, Solution renamedSolution)
        {
            await _threadingService.SwitchToUIThread();
            return ws.TryApplyChanges(renamedSolution);
        }
    }
}
