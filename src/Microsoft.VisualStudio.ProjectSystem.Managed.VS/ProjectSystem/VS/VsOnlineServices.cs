// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IVsOnlineServices))]
    internal class VsOnlineServices : IVsOnlineServices
    {
        public bool ConnectedToVSOnline => KnownUIContexts.CloudEnvironmentConnectedContext.IsActive;
    }
}
