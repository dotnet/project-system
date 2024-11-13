// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Allows subscribing to solution build manager events via the <see cref="IVsUpdateSolutionEvents"/>
    /// family of event handler interfaces.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.System)]
    internal interface ISolutionBuildManager
    {
        /// <summary>
        /// Creates a new subscription for build events that will call back via <paramref name="eventListener" />.
        /// </summary>
        /// <param name="eventListener">The callback for events. Note that it may also implement additional version(s) of this interface.</param>
        /// <returns>An object that unsubscribes when disposed.</returns>
        Task<IAsyncDisposable> SubscribeSolutionEventsAsync(IVsUpdateSolutionEvents eventListener);

        int QueryBuildManagerBusy();

        uint QueryBuildManagerBusyEx();

        void SaveDocumentsBeforeBuild(IVsHierarchy hierarchy, uint itemId = unchecked((uint)VSConstants.VSITEMID.Root), uint docCookie = 0);

        void CalculateProjectDependencies();

        IVsHierarchy[] GetProjectDependencies(IVsHierarchy hierarchy);

        void StartUpdateSpecificProjectConfigurations(IVsHierarchy[] hierarchy, uint[] buildFlags, uint dwFlags);
    }
}
