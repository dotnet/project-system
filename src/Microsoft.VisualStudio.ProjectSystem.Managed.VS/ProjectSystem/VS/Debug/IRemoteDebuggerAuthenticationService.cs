// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal interface IRemoteDebuggerAuthenticationService
    {
        IEnumerable<IRemoteAuthenticationProvider> GetRemoteAuthenticationModes();
        IRemoteAuthenticationProvider? FindProviderForAuthenticationMode(string remoteAuthenticationMode);
        bool ShowRemoteDiscoveryDialog(ref string remoteDebugMachine, ref IRemoteAuthenticationProvider? remoteAuthenticationProvider);
    }
}
