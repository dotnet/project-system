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

        /// <summary>
        /// Text for the menu item that fixes the problem with the project
        /// </summary>
        public override string CommandTitle => "Install Missing Component(s)...";

        /// <summary>
        /// A brief description of why the project can't be loaded, displayed in parentheses after the project name
        /// </summary>
        public virtual string UnloadReason => "Components missing";

        /// <summary>
        /// A longer description of why the project can't be loaded, displayed as a child node of the project
        /// </summary>
        public virtual string UnloadDescription => "Visual Studio components required to load this project are missing.";

        public override object? GetProperty(uint prop) => ((__VSPTDPROPID)prop) switch
        {
            __VSPTDPROPID.VSPTDPROPID_MissingPrerequisites => true,
            __VSPTDPROPID.VSPTDPROPID_AcquisitionComponents => _component,
            __VSPTDPROPID.VSPTDPROPID_UnloadInfoLine => UnloadDescription,
            __VSPTDPROPID.VSPTDPROPID_UnloadTitle => UnloadReason,
            _ => base.GetProperty(prop),
        };
    }
}
