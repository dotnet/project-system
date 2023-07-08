// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IRemoteDebuggerAuthenticationService
    {
        IEnumerable<IRemoteAuthenticationProvider> GetRemoteAuthenticationModes();
        IRemoteAuthenticationProvider? FindProviderForAuthenticationMode(string remoteAuthenticationMode);
        bool ShowRemoteDiscoveryDialog(ref string remoteDebugMachine, ref IRemoteAuthenticationProvider? remoteAuthenticationProvider);
    }
}
