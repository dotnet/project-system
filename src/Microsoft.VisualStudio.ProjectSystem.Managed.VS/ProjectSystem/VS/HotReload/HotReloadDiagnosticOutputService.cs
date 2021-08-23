// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [Export(typeof(IHotReloadDiagnosticOutputService))]
    internal class HotReloadOutputWindowPaneProvider : IHotReloadDiagnosticOutputService
    {
        // This is the well-known GUID for the Hot Reload output window pane.
        private static readonly Guid s_hotReloadOutputWindowPaneGuid = new("{7BD3920C-C53C-40ED-8D79-4D2F601BEFB0}");

        private readonly IProjectThreadingService _threadingService;
        private readonly IVsUIService<IVsOutputWindow?> _outputWindow;
        private readonly AsyncLazy<IVsOutputWindowPane?> _outputWindowPane;

        [ImportingConstructor]
        public HotReloadOutputWindowPaneProvider(IProjectThreadingService threadingService, IVsUIService<SVsOutputWindow, IVsOutputWindow?> outputWindow)
        {
            _threadingService = threadingService;
            _outputWindow = outputWindow;
            _outputWindowPane = new AsyncLazy<IVsOutputWindowPane?>(CreateOutputWindowPaneAsync, threadingService.JoinableTaskFactory);
        }

        private async Task<IVsOutputWindowPane?> CreateOutputWindowPaneAsync()
        {
            await _threadingService.SwitchToUIThread();

            IVsOutputWindow? outputWindow = _outputWindow.Value;
            if (outputWindow is null)
            {
                return null;    // Command-line build
            }

            Guid activePane = outputWindow.GetActivePane();

            IVsOutputWindowPane pane = CreateProjectOutputWindowPane(outputWindow);

            // Creating a pane causes it to be "active", reset the active pane back to the previously active pane
            if (activePane != Guid.Empty)
            {
                outputWindow.ActivatePane(activePane);
            }

            return pane;
        }

        private static IVsOutputWindowPane CreateProjectOutputWindowPane(IVsOutputWindow outputWindow)
        {
            Guid paneGuid = s_hotReloadOutputWindowPaneGuid;

            Verify.HResult(outputWindow.CreatePane(ref paneGuid, VSResources.HotReloadOutputWindowPaneName, fInitVisible: 1, fClearWithSolution: 0));

            Verify.HResult(outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane));

            return pane;
        }

        public async Task WriteLineAsync(string outputMessage)
        {
            await _threadingService.SwitchToUIThread();

            IVsOutputWindowPane? pane = await _outputWindowPane.GetValueAsync();
            if (pane is not null)
            {
                pane.OutputString(outputMessage + Environment.NewLine);
            }
        }
    }
}
