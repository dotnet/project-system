// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    [Export(typeof(IHotReloadDiagnosticOutputService))]
    internal class HotReloadDiagnosticOutputService : IHotReloadDiagnosticOutputService
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IHotReloadLogger _hotReloadLogger;

        [ImportingConstructor]
        public HotReloadDiagnosticOutputService(IProjectThreadingService threadingService, IHotReloadLogger hotReloadLogger)
        {
            _threadingService = threadingService;
            _hotReloadLogger = hotReloadLogger;
        }

        public void WriteLine(HotReloadLogMessage hotReloadLogMessage, CancellationToken cancellationToken)
        {
            _threadingService.RunAndForget(() => _hotReloadLogger.LogAsync(hotReloadLogMessage, cancellationToken).AsTask(), unconfiguredProject: null);
        }
        
        public static uint GetProcessId(Process? process = null)
        {
            return (uint)(process?.Id ?? 0);
        }
    }
}
