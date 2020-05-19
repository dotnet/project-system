// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    internal class DesktopPlatformTargetDescription : RetargetProjectTargetDescriptionBase
    {
        private readonly Guid _targetDescriptionId = new Guid("08A73DA5-B2C7-4DEF-8E1F-5D7C903ED3A3");

        public override Guid TargetId
        {
            get { return _targetDescriptionId; }
        }

        public override bool Supported
        {
            get { return true; }
        }

        public override uint Order
        {
            get { return 1000; }
        }

        public override string DisplayName
        {
            get { return "Missing Desktop Platform"; }
        }

        public string? NewTargetPlatformName { get; private set; }

        public override Array GetRetargetParameters()
        {
            return new[] { "DesktopPlatform" };
        }

        public override string GetRetargetParameterDisplayName(string parameter)
        {
            return "Platform";
        }

        public override Array GetPossibleParameterValues(string parameter)
        {
            return new string[] { "WindowsForms", "WPF" };
        }

        public override string GetParameterValue(string parameter)
        {
            return parameter;
        }

        public override void PutParameterValue(string parameter, string pValue)
        {
            NewTargetPlatformName = pValue;
        }

        public override string GetValueDisplayName(string parameter, string pValue)
        {
            return pValue;
        }

        public override void ResetSelectedValues()
        {
            NewTargetPlatformName = null;
        }

        public override object? GetProperty(uint propertyId)
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
                    return "No Desktop Platform selected";
            }

            return base.GetProperty(propertyId);
        }
    }
}


