// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal abstract class InstallerComponentMissingTargetDescriptionBase : TargetDescriptionBase
    {
        private readonly string _component;

        protected InstallerComponentMissingTargetDescriptionBase(string component)
        {
            _component = component;
        }

        public override bool Supported => false;

        public override string CommandTitle => "Install Missing Component(s)...";

        public override object? GetProperty(uint prop) => ((__VSPTDPROPID)prop) switch
        {
            __VSPTDPROPID.VSPTDPROPID_MissingPrerequisites => true,
            __VSPTDPROPID.VSPTDPROPID_AcquisitionComponents => _component,
            _ => base.GetProperty(prop),
        };
    }
}
