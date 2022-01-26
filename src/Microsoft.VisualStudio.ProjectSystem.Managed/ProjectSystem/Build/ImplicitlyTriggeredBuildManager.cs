// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
    /// <summary>
    /// Build manager for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
    /// </summary>
    [Export(typeof(IImplicitlyTriggeredBuildManager))]
    [Export(typeof(IImplicitlyTriggeredBuildState))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed partial class ImplicitlyTriggeredBuildManager : IImplicitlyTriggeredBuildManager, IImplicitlyTriggeredBuildState
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private bool _isImplicitlyTriggeredBuild;

        public bool IsImplicitlyTriggeredBuild => _isImplicitlyTriggeredBuild;

        public void OnBuildStart()
            => _isImplicitlyTriggeredBuild = true;

        public void OnBuildEndOrCancel()
            => _isImplicitlyTriggeredBuild = false;
    }
}
