// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Build manager for implicitly triggered builds from commands such as Run/Debug Tests, Start Debugging, etc.
    /// </summary>
    [Export(typeof(IImplicitlyTriggeredBuildManager))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed partial class ImplicitlyTriggeredBuildManager : IImplicitlyTriggeredBuildManager
    {
        public void OnBuildStart()
            => GlobalPropertiesStore.Instance.OnBuildStart();

        public void OnBuildEndOrCancel()
            => GlobalPropertiesStore.Instance.OnBuildEndOrCancel();
    }
}
