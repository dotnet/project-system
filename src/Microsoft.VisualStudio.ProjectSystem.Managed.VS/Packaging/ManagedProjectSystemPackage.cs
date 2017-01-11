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
                                                  ProjectCapability.OpenProjectFile + "; " +
                                                  ProjectCapabilities.ProjectConfigurationsInferredFromUsage + "; " +
                                                  ProjectCapabilities.LanguageService;

        public ManagedProjectSystemPackage()
        {
        }
    }
}
