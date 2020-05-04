// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal class DesktopPlatformTargetDescription : IVsProjectTargetDescription2
    {
        private readonly Guid _targetDescriptionId = Guid.NewGuid();
        private const string RetargetComboBoxProperty = "DesktopPlatform";

        public DesktopPlatformTargetDescription()
        {
        }

        public Guid TargetId
        {
            get { return _targetDescriptionId; }
        }

        public bool Supported
        {
            get { return true; }
        }

        public uint Order
        {
            get { return 1000; }
        }

        public string DisplayName
        {
            get { return "Missing Desktop Platform"; }
        }

        public string? NewTargetPlatformName { get; private set; }

        public Array GetRetargetParameters()
        {
            return new[] { RetargetComboBoxProperty };
        }

        public string GetRetargetParameterDisplayName(string parameter)
        {
            return "Platform";
        }

        public Array GetPossibleParameterValues(string parameter)
        {
            return new string[] { "WindowsForms", "WPF" };
        }

        public string GetParameterValue(string parameter)
        {
            return parameter;
        }

        public void PutParameterValue(string parameter, string pValue)
        {
            NewTargetPlatformName = pValue;
        }

        public string GetValueDisplayName(string parameter, string pValue)
        {
            return pValue;
        }

        public void ResetSelectedValues()
        {
            NewTargetPlatformName = null;
        }

        public object? GetProperty(uint propertyId)
        {
            switch (propertyId)
            {
                case (uint)__VSPTDPROPID.VSPTDPROPID_MissingPrerequisites:
                    return false;

                case (uint)__VSPTDPROPID.VSPTDPROPID_AcquisitionSetupDriver:
                    return null;

                case (uint)__VSPTDPROPID.VSPTDPROPID_AcquisitionComponents:
                    return null;

                case (uint)__VSPTDPROPID.VSPTDPROPID_AcquisitionCommandTitle:
                    return DisplayName;

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectRetargetingDescription:
                    return "You have selected the Windows Desktop SDK but you have not selected to target Winforms or WPF. You must do this to load the project.";

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectUnloadUntilRetargetedTitle:
                    return "unload until retargets";

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectRetargetingTitle:
                    return "REtargetin";
            }

            return null;
        }
    }
}


