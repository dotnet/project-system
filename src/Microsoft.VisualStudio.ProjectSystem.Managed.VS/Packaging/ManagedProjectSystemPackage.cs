// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";
        public const string ManagedProjectSystemCommandSet = "{568ABDF7-D522-474D-9EED-34B5E5095BA5}";
        public const long EditProjectFileCmdId = 0x1001;
        public const long GenerateNuGetPackageProjectContextMenuCmdId = 0x2000;
        public const long GenerateNuGetPackageTopLevelBuildCmdId = 0x2001;
        public const string DefaultCapabilities = ProjectCapability.AppDesigner + "; " +
                                                  ProjectCapability.EditAndContinue + "; " +
                                                  ProjectCapability.HandlesOwnReload + "; " +
                                                  ProjectCapability.OpenProjectFile;

        public const long RunCodeAnalysisTopLevelBuildMenuCmdId = 0x066f;
        public const long RunCodeAnalysisProjectContextMenuCmdId = 0x0670;
        public const string CodeAnalysisPackageGuid = "B20604B0-72BC-4953-BB92-95BF26D30CFA";
        public const string VSStd2KCommandSet = "1496A755-94DE-11D0-8C3F-00C04FC2AAE2";

        public ManagedProjectSystemPackage()
        {
        }
    }
}
