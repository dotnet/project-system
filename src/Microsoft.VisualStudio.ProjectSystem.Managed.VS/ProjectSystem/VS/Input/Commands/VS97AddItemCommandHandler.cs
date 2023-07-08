// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard97)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VS97AddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly ImmutableDictionary<long, ImmutableArray<TemplateDetails>> s_templateDetails = ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
            //                     Command Id                                Capability                      DirNamePackageGuid          DirNameResourceId                                       TemplateNameResourceId
            .CreateTemplateDetails(VisualStudioStandard97CommandId.AddClass, ProjectCapability.CSharp,       LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWCSharpCLASS )

            .CreateTemplateDetails(VisualStudioStandard97CommandId.AddClass, ProjectCapability.VisualBasic,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_CLASS            );

        protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => s_templateDetails;

        [ImportingConstructor]
        public VS97AddItemCommandHandler(ConfiguredProject configuredProject, IAddItemDialogService addItemDialogService, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(configuredProject, addItemDialogService, vsShell)
        {
        }
    }
}
