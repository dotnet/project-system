using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal partial class AbstractAddItemCommandHandler
    {
        protected class CommandDetails
        {
            public Guid DirNamePackageGuid;
            public uint DirNameResourceId;
            public Guid TemplateNamePackageGuid;
            public uint TemplateNameResourceId;

            /// <summary>
            /// Initializes a new instance of CommandDetails assuming that both strings come from the same package
            /// </summary>
            public CommandDetails(Guid packageGuid, uint dirName, uint templateName)
                : this(packageGuid, dirName, packageGuid, templateName)
            {
            }

            /// <summary>
            /// Initializes a new instance of CommandDetails allowing for each string to come from different packages
            /// </summary>
            public CommandDetails(Guid dirNamePackageGuid, uint dirName, Guid templateNamePackageGuid, uint templateName)
            {
                DirNamePackageGuid = dirNamePackageGuid;
                DirNameResourceId = dirName;
                TemplateNamePackageGuid = templateNamePackageGuid;
                TemplateNameResourceId = templateName;
            }
        }
    }
}
