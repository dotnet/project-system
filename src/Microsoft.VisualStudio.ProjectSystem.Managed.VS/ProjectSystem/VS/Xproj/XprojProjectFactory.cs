// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid(ProjectType.LegacyXProj)]
    internal sealed class XprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory4
    {
        public void UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger logger,
            out uint upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            bool isXproj = fileName.EndsWith(".xproj");

            // If the project is an xproj, then indicate it is deprecated. If it isn't, then there's nothing we can do with it.
            upgradeRequired = isXproj
                ? (uint)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED
                : (uint)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            if (isXproj && logger != null)
            {
                // Log a message explaining that the project cannot be automatically upgraded
                // and how to perform the upgrade manually.
                string projectName = Path.GetFileNameWithoutExtension(fileName);
                logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, fileName, VSResources.XprojNotSupported);
            }

            migratedProjectFactory = GetType().GUID;
            upgradeProjectCapabilityFlags = 0;
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // Should not be called
            throw new NotImplementedException();
        }
    }
}
