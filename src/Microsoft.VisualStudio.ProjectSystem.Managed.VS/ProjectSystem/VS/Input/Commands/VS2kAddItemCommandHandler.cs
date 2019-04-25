// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard2k)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VS2kAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        protected override Dictionary<long, List<TemplateDetails>> GetTemplateDetails() => new Dictionary<long, List<TemplateDetails>>
        {
           // Command Id                                        Capability                      AdditionalCapability                DirNamePackageGuid          DirNameResourceId                                       TemplateNameResourceId
            { VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCWIN32FORM  },
            { VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWUSERCONTROL   },
            { VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.CSharp,       ProjectCapability.WindowsForms,     LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWWFCCOMPONENT  },

            { VisualStudioStandard2KCommandId.AddForm,          ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_WINFORM            },
            { VisualStudioStandard2KCommandId.AddUserControl,   ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_USERCTRL           },
            { VisualStudioStandard2KCommandId.AddComponent,     ProjectCapability.VisualBasic,  ProjectCapability.WindowsForms,     LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_COMPONENT          },

            { VisualStudioStandard2KCommandId.AddModule,        ProjectCapability.VisualBasic,                                      LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_MODULE             },
        };

        [ImportingConstructor]
        public VS2kAddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}

