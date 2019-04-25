using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard97)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VS97AddItemCommandHandler : AbstractAddItemCommandHandler
    {
        protected override Dictionary<long, List<TemplateDetails>> GetTemplateDetails() => new Dictionary<long, List<TemplateDetails>>
        {
           // Command Id                                Capability                      DirNamePackageGuid          DirNameResourceId                                       TemplateNameResourceId
            { VisualStudioStandard97CommandId.AddClass, ProjectCapability.CSharp,       LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_PROJECTITEMTYPE_STR,  LegacyCSharpStringResourceIds.IDS_TEMPLATE_NEWCSharpCLASS   },

            { VisualStudioStandard97CommandId.AddClass, ProjectCapability.VisualBasic,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_VBPROJECTFILES,     LegacyVBStringResourceIds.IDS_VSDIR_ITEM_CLASS              },
        };

        [ImportingConstructor]
        public VS97AddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsService<SVsShell, IVsShell> vsShell)
            : base(projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}

