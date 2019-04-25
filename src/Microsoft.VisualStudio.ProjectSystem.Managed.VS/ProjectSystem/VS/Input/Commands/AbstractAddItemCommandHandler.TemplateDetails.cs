using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal partial class AbstractAddItemCommandHandler
    {
        internal class TemplateDetails
        {
            public string CapabilityCheck;
            public Guid DirNamePackageGuid;
            public uint DirNameResourceId;
            public Guid TemplateNamePackageGuid;
            public uint TemplateNameResourceId;

            /// <summary>
            /// Initializes a new instance of CommandDetails allowing for each string to come from different packages
            /// </summary>
            public TemplateDetails(string capabilityCheck, Guid dirNamePackageGuid, uint dirName, Guid templateNamePackageGuid, uint templateName)
            {
                CapabilityCheck = capabilityCheck;
                DirNamePackageGuid = dirNamePackageGuid;
                DirNameResourceId = dirName;
                TemplateNamePackageGuid = templateNamePackageGuid;
                TemplateNameResourceId = templateName;
            }
        }
    }
}
