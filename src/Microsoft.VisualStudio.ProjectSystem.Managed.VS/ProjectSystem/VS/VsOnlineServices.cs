// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IVsOnlineServices))]
    internal class VsOnlineServices : IVsOnlineServices
    {
        public bool ConnectedToVSOnline => KnownUIContexts.CloudEnvironmentConnectedContext.IsActive;
    }
}
