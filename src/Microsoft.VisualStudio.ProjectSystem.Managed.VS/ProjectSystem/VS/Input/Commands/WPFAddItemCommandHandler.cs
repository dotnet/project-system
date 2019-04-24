using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.WPF)]
    [AppliesTo(ProjectCapability.WPF)]
    internal partial class WPFAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly Guid s_wpfPackage = new Guid("{b3bae735-386c-4030-8329-ef48eeda4036}");

        // from VS: src\vsproject\cool\coolpkg\resource.h
        protected enum LegacyCSharpStrings : uint
        {
            IDS_TEMPLATE_DIRLOCALITEMS = 2339,
        }

        // from VS: src\vsproject\vb\vbprj\vbprjstr.h
        protected enum LegacyVBStrings : uint
        {
            IDS_VSDIR_CLIENTPROJECTITEMS = 3081,
        }

        // from VS: src\vsproject\fidalgo\WPF\Flavor\WPFFlavor\Guids.cs
        private enum CommandIds : long
        {
            AddWPFWindow = 0x100,
            AddWPFPage = 0x200,
            AddWPFUserControl = 0x300,
            AddWPFResourceDictionary = 0x400,
            WPFWindow = 0x600,
            WPFPage = 0x700,
            WPFUserControl = 0x800,
            WPFResourceDictionary = 0x900,
        }

        // from VS: src\vsproject\fidalgo\WPF\Flavor\WPFFlavor\WPFProject.cs
        private enum TemplateNames : uint
        {
            WPFPage = 4658,
            WPFResourceDictionary = 4662,
            WPFUserControl = 4664,
            WPFWindow = 4666,
        }

        protected override Dictionary<long, CommandDetails> GetCSharpCommands() => new Dictionary<long, CommandDetails>
        {
            { (long)CommandIds.AddWPFWindow, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFWindow) },
            { (long)CommandIds.WPFWindow, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFWindow) },
            { (long)CommandIds.AddWPFPage, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFPage) },
            { (long)CommandIds.WPFPage, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFPage) },
            { (long)CommandIds.AddWPFUserControl, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFUserControl) },
            { (long)CommandIds.WPFUserControl, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFUserControl) },
            { (long)CommandIds.AddWPFResourceDictionary, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFResourceDictionary) },
            { (long)CommandIds.WPFResourceDictionary, new CommandDetails(LegacyCSharpPackageGuid, (uint)LegacyCSharpStrings.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, (uint)TemplateNames.WPFResourceDictionary) },
        };

        protected override Dictionary<long, CommandDetails> GetVBCommands() => new Dictionary<long, CommandDetails>
        {
            { (long)CommandIds.AddWPFWindow, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFWindow) },
            { (long)CommandIds.WPFWindow, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFWindow) },
            { (long)CommandIds.AddWPFPage, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFPage) },
            { (long)CommandIds.WPFPage, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFPage) },
            { (long)CommandIds.AddWPFUserControl, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFUserControl) },
            { (long)CommandIds.WPFUserControl, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFUserControl) },
            { (long)CommandIds.AddWPFResourceDictionary, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFResourceDictionary) },
            { (long)CommandIds.WPFResourceDictionary, new CommandDetails(LegacyVBPackageGuid, (uint)LegacyVBStrings.IDS_VSDIR_CLIENTPROJECTITEMS, s_wpfPackage, (uint)TemplateNames.WPFResourceDictionary) },
        };

        [ImportingConstructor]
        public WPFAddItemCommandHandler(IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsService<SVsShell, IVsShell> vsShell)
            : base(projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}
