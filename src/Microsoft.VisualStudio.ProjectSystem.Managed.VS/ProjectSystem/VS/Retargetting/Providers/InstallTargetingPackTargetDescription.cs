// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting
{
    internal class InstallTargetingPackTargetDescription : InstallerComponentMissingTargetDescriptionBase
    {
        private readonly Guid _targetDescriptionId = new Guid("0ABC0204-20D6-4A24-86F7-2F953B929C03");

        public InstallTargetingPackTargetDescription(string component)
            : base(component)
        {
        }

        public override Guid TargetId => _targetDescriptionId;

        public override string DisplayName => VSResources.InstallTargetingPackDisplayName;

        public override string CommandTitle => VSResources.InstallTargetingPackCommandTitle;

        public override string UnloadReason => VSResources.InstallTargetingPackUnloadReason;

        public override string UnloadDescription => VSResources.InstallTargetingPackUnloadDescription;

        public override Guid SetupDriver => VSConstants.SetupDrivers.SetupDriver_VS;
    }
}
