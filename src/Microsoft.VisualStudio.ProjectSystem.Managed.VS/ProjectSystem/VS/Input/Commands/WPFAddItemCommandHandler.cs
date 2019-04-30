// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ExportCommandGroup(CommandGroup.WPF)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal partial class WPFAddItemCommandHandler : AbstractAddItemCommandHandler
    {
        private static readonly Guid s_wpfPackage = new Guid("{b3bae735-386c-4030-8329-ef48eeda4036}");

        protected override Dictionary<long, List<TemplateDetails>> GetTemplateDetails() => new Dictionary<long, List<TemplateDetails>>
        {
           // Command Id                                Capability                      ExtraCapability         DirNamePackageGuid          DirNameResourceId                                           TemplateName    TemplateNameResourceId
           //                                                                                                                                                                   PackageGuid
            { WPFCommandId.AddWPFWindow,                ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFWindow              },
            { WPFCommandId.WPFWindow,                   ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFWindow              },
            { WPFCommandId.AddWPFPage,                  ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFPage                },
            { WPFCommandId.WPFPage,                     ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFPage                },
            { WPFCommandId.AddWPFUserControl,           ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFUserControl         },
            { WPFCommandId.WPFUserControl,              ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFUserControl         },
            { WPFCommandId.AddWPFResourceDictionary,    ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFResourceDictionary  },
            { WPFCommandId.WPFResourceDictionary,       ProjectCapability.CSharp,       ProjectCapability.WPF,  LegacyCSharpPackageGuid,    LegacyCSharpStringResourceIds.IDS_TEMPLATE_DIRLOCALITEMS,   s_wpfPackage,   WPFTemplateNames.WPFResourceDictionary  },

            { WPFCommandId.AddWPFWindow,                ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFWindow              },
            { WPFCommandId.WPFWindow,                   ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFWindow              },
            { WPFCommandId.AddWPFPage,                  ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFPage                },
            { WPFCommandId.WPFPage,                     ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFPage                },
            { WPFCommandId.AddWPFUserControl,           ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFUserControl         },
            { WPFCommandId.WPFUserControl,              ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFUserControl         },
            { WPFCommandId.AddWPFResourceDictionary,    ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFResourceDictionary  },
            { WPFCommandId.WPFResourceDictionary,       ProjectCapability.VisualBasic,  ProjectCapability.WPF,  LegacyVBPackageGuid,        LegacyVBStringResourceIds.IDS_VSDIR_CLIENTPROJECTITEMS,     s_wpfPackage,   WPFTemplateNames.WPFResourceDictionary  },
        };

        [ImportingConstructor]
        public WPFAddItemCommandHandler(ConfiguredProject configuredProject, IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
            : base(configuredProject, projectTree, projectVsServices, addItemDialog, vsShell)
        {
        }
    }
}
