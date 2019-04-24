using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.VisualStudioStandard2k)]
    [AppliesTo(ProjectCapability.WindowsForms)]
    internal class WinFormsAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        // from VS: src\vsproject\cool\coolpkg\resource.h
        protected enum LegacyCSharpStrings : uint
        {
            IDS_TEMPLATE_NEWWFCWIN32FORM = 2237,
            IDS_TEMPLATE_NEWWFCCOMPONENT = 2246,
            IDS_TEMPLATE_NEWUSERCONTROL = 2295,
            IDS_PROJECTITEMTYPE_STR = 2346,
        }

        // from VS: src\vsproject\vb\vbprj\vbprjstr.h
        protected enum LegacyVBStrings : uint
        {
            IDS_VSDIR_ITEM_COMPONENT = 3024,
            IDS_VSDIR_ITEM_USERCTRL = 3048,
            IDS_VSDIR_ITEM_WINFORM = 3050,
            IDS_VSDIR_VBPROJECTFILES = 3082,
        }

        // C#
        protected override Dictionary<long, CommandDetails> GetCSharpCommands() => new Dictionary<long, CommandDetails>
        {
            { VisualStudioStandard2KCommandId.AddForm, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_PROJECTITEMTYPE_STR, (uint)LegacyCSharpStrings.IDS_TEMPLATE_NEWWFCWIN32FORM) },
            { VisualStudioStandard2KCommandId.AddUserControl, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_PROJECTITEMTYPE_STR, (uint)LegacyCSharpStrings.IDS_TEMPLATE_NEWUSERCONTROL) },
            { VisualStudioStandard2KCommandId.AddComponent, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_PROJECTITEMTYPE_STR, (uint)LegacyCSharpStrings.IDS_TEMPLATE_NEWWFCCOMPONENT) },
        };

        // VB
        protected override Dictionary<long, CommandDetails> GetVBCommands() => new Dictionary<long, CommandDetails>
        {
            { VisualStudioStandard2KCommandId.AddForm,new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_VBPROJECTFILES, (uint)LegacyVBStrings.IDS_VSDIR_ITEM_WINFORM) },
            { VisualStudioStandard2KCommandId.AddUserControl, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_VBPROJECTFILES, (uint)LegacyVBStrings.IDS_VSDIR_ITEM_USERCTRL) },
            { VisualStudioStandard2KCommandId.AddComponent, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_VBPROJECTFILES, (uint)LegacyVBStrings.IDS_VSDIR_ITEM_COMPONENT) },
        };

        [ImportingConstructor]
        public WinFormsAddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsService<SVsShell, IVsShell> vsShell)
            : base(projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}

