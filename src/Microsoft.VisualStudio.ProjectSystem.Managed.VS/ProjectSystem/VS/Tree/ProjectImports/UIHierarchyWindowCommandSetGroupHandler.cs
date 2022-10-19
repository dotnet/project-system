// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.ProjectImports;

[ExportCommandGroup(CMDSETID.UIHierarchyWindowCommandSet_string)]
[AppliesTo(ProjectCapability.ProjectImportsTree)]
[Order(Order.BeforeDefault)]
internal sealed class UIHierarchyWindowCommandSetGroupHandler : VsProjectImportsCommandGroupHandlerBase
{
    [ImportingConstructor]
    public UIHierarchyWindowCommandSetGroupHandler(
#pragma warning disable RS0030 // Do not used banned APIs
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
#pragma warning restore RS0030 // Do not used banned APIs
        ConfiguredProject configuredProject,
        IVsUIService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> uiShellOpenDocument,
        IVsUIService<SVsExternalFilesManager, IVsExternalFilesManager> externalFilesManager,
        IVsUIService<IOleServiceProvider> oleServiceProvider)
    : base(serviceProvider, configuredProject, uiShellOpenDocument, externalFilesManager, oleServiceProvider)
    {
    }

    protected override bool IsOpenCommand(long commandId)
    {
        return (VsUIHierarchyWindowCmdIds)commandId switch
        {
            VsUIHierarchyWindowCmdIds.UIHWCMDID_DoubleClick => true,
            VsUIHierarchyWindowCmdIds.UIHWCMDID_EnterKey => true,
            _ => false
        };
    }

    protected override bool IsOpenWithCommand(long commandId) => false;
}

