// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Guid(ProjectType.LegacyXProj)]
    internal sealed class XprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory, IVsProjectUpgradeViaFactory4
    {
        public int UpgradeProject(
            string xprojLocation,
            uint upgradeFlags,
            string backupDirectory,
            out string migratedProjectFileLocation,
            IVsUpgradeLogger logger,
            out int upgradeRequired,
            out Guid migratedProjectGuid)
        {
            migratedProjectFileLocation = default;
            upgradeRequired = default;
            migratedProjectGuid = default;
            return VSConstants.S_FALSE;
        }

        public int UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger logger,
            out int upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            bool isXproj = fileName.EndsWith(".xproj");

            // If the project is an xproj, then indicate it is deprecated. If it isn't, then there's nothing we can do with it.
            upgradeRequired = isXproj
                ? (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED
                : (int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR;

            if (isXproj && logger != null)
            {
                // Log a message explaining that the project cannot be automatically upgraded
                // and how to perform the upgrade manually.
                string projectName = Path.GetFileNameWithoutExtension(fileName);
                logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, fileName, VSResources.XprojNotSupported);
            }

            migratedProjectFactory = GetType().GUID;
            upgradeProjectCapabilityFlags = 0;
            return HResult.OK;
        }

        public void UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger logger,
            out uint upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            UpgradeProject_CheckOnly(fileName, logger, out int iUpgradeRequired, out migratedProjectFactory, out upgradeProjectCapabilityFlags);
            upgradeRequired = unchecked((uint)iUpgradeRequired);
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // Should not be called
            throw new NotImplementedException();
        }

        public int GetSccInfo(string bstrProjectFileName, out string pbstrSccProjectName, out string pbstrSccAuxPath, out string pbstrSccLocalPath, out string pbstrProvider)
        {
            // Should not be called
            throw new NotImplementedException();
        }
    }
}
