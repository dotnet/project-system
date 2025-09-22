// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

internal partial class ProjectRetargetHandler
{

    internal abstract class TargetDescriptionBase : IVsProjectTargetDescription
    {
        private readonly string _description;

        private readonly string? _nextStepGuidanceLink;

        private readonly bool _canRetarget;

        public TargetDescriptionBase(Guid targetId, string displayName, uint order, bool supported, string description, bool canRetarget, string? guidanceLink)
        {
            TargetId = targetId;
            DisplayName = displayName;
            Order = order;
            Supported = supported;

            _description = description;
            _canRetarget = canRetarget;
            _nextStepGuidanceLink = guidanceLink;
        }

        public Guid TargetId { get; private set; }

        public string DisplayName { get; private set; }

        public uint Order { get; private set; }

        public bool Supported { get; private set; }

        public object GetProperty(uint prop)
        {
            switch (prop)
            {
                case (uint)__VSPTDPROPID.VSPTDPROPID_DoNotAdvertiseRetarget:
                    return !_canRetarget;

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectRetargetingTitle:
                    return DisplayName;

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectRetargetingDescription:
                    return _description;

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectRetargetingGuidanceLink:
                    return _nextStepGuidanceLink!;

                case (uint)__VSPTDPROPID.VSPTDPROPID_ProjectUnloadUntilRetargetedTaskPriority:
                    return (uint)__RETARGET_TASK_PRIORITY.RTP_High;

                case (uint)__VSPTDPROPIDPrivate.VSPTDPROPID_SupportsNewUI:
                    return true;

                default:
                    return null!;
            }
        }

        /// <summary>
        /// Private definition for a properties in a IVsProjectTargetDescription
        /// Extends the properties specified in __VSPTDPROPID
        /// </summary>
#pragma warning disable IDE1006 // Naming Styles
        public enum __VSPTDPROPIDPrivate
#pragma warning restore IDE1006 // Naming Styles
        {
            VSPTDPROPID_SupportsNewUI = 1001
        }
    }
}
