// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.WPF)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class WPFAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly Guid s_wpfPackage = new Guid("{b3bae735-386c-4030-8329-ef48eeda4036}");

        private static readonly ImmutableDictionary<long, ImmutableArray<TemplateDetails>> s_templateDetails = ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
            //                     Command Id                             Capabilities                                          DirNamePackageGuid       DirNameResourceId                                         TemplateName  TemplateNameResourceId
            //                                                                                                                                                                                                     PackageGuid
            .CreateTemplateDetails(WPFCommandId.AddWPFWindow,             ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFWindow             )
            .CreateTemplateDetails(WPFCommandId.AddWPFWindow,             ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFWindow             )
            .CreateTemplateDetails(WPFCommandId.WPFWindow,                ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFWindow             )
            .CreateTemplateDetails(WPFCommandId.AddWPFPage,               ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFPage               )
            .CreateTemplateDetails(WPFCommandId.WPFPage,                  ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFPage               )
            .CreateTemplateDetails(WPFCommandId.AddWPFUserControl,        ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFUserControl        )
            .CreateTemplateDetails(WPFCommandId.WPFUserControl,           ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFUserControl        )
            .CreateTemplateDetails(WPFCommandId.AddWPFResourceDictionary, ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFResourceDictionary )
            .CreateTemplateDetails(WPFCommandId.WPFResourceDictionary,    ProjectCapability.CSharp,      ProjectCapability.WPF, LegacyCSharpPackageGuid, LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS, s_wpfPackage, WPFTemplateNames.WPFResourceDictionary )

            .CreateTemplateDetails(WPFCommandId.AddWPFWindow,             ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFWindow             )
            .CreateTemplateDetails(WPFCommandId.WPFWindow,                ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFWindow             )
            .CreateTemplateDetails(WPFCommandId.AddWPFPage,               ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFPage               )
            .CreateTemplateDetails(WPFCommandId.WPFPage,                  ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFPage               )
            .CreateTemplateDetails(WPFCommandId.AddWPFUserControl,        ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFUserControl        )
            .CreateTemplateDetails(WPFCommandId.WPFUserControl,           ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFUserControl        )
            .CreateTemplateDetails(WPFCommandId.AddWPFResourceDictionary, ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFResourceDictionary )
            .CreateTemplateDetails(WPFCommandId.WPFResourceDictionary,    ProjectCapability.VisualBasic, ProjectCapability.WPF, LegacyVBPackageGuid,     LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,   s_wpfPackage, WPFTemplateNames.WPFResourceDictionary );

        protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => s_templateDetails;

        [ImportingConstructor]
        public WPFAddItemCommandHandler(ConfiguredProject configuredProject, IAddItemDialogService addItemDialogService, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(configuredProject, addItemDialogService, vsShell)
        {
        }
    }
}
