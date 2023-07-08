// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard2k)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VS2kAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly ImmutableDictionary<long, ImmutableArray<TemplateDetails>> s_templateDetails = ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
            //                     Command Id                                        Capabilities                                                        DirNamePackageGuid          DirNameResourceId                                       TemplateNameResourceId
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCWIN32FORM  )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWUSERCONTROL   )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCCOMPONENT  )

            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_WINFORM            )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_USERCTRL           )
            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_COMPONENT          )

            .CreateTemplateDetails(VisualStudioStandard2KCommandId.AddModule,        ProjectCapability.VisualBasic,                                      LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_MODULE             );

        protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => s_templateDetails;

        [ImportingConstructor]
        public VS2kAddItemCommandHandler(ConfiguredProject configuredProject, IAddItemDialogService addItemDialogService, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(configuredProject, addItemDialogService, vsShell)
        {
        }
    }
}
