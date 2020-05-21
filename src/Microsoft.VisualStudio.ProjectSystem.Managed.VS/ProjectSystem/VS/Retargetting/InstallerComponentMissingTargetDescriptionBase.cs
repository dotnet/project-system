// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal abstract class InstallerComponentMissingTargetDescriptionBase : TargetDescriptionBase
    {
        public override object? GetProperty(uint prop)
        {
            switch ((__VSPTDPROPID)prop)
            {
                case __VSPTDPROPID.VSPTDPROPID_MissingPrerequisites:
                    return true;
            }
            return base.GetProperty(prop);
        }
    }
}
