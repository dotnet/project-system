// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal partial class AbstractAddItemCommandHandler
    {
        internal class TemplateDetails
        {
            public string AppliesTo { get; }
            public uint DirNameResourceId { get; }
            public Guid DirNamePackageGuid { get; }
            public uint TemplateNameResourceId { get; }
            public Guid TemplateNamePackageGuid { get; }

            /// <summary>
            /// Initializes a new instance of CommandDetails allowing for each string to come from different packages
            /// </summary>
            public TemplateDetails(string appliesTo, Guid dirNamePackageGuid, uint dirName, Guid templateNamePackageGuid, uint templateName)
            {
                AppliesTo = appliesTo;
                DirNamePackageGuid = dirNamePackageGuid;
                DirNameResourceId = dirName;
                TemplateNamePackageGuid = templateNamePackageGuid;
                TemplateNameResourceId = templateName;
            }
        }
    }
}
