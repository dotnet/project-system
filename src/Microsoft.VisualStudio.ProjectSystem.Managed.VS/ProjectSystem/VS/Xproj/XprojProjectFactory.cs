// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Export(typeof(IPackageService))]
    [Guid(ProjectType.LegacyXProj)]
    internal sealed class XprojProjectFactory : FlavoredProjectFactoryBase, IVsProjectUpgradeViaFactory4, IPackageService
    {
        public void UpgradeProject_CheckOnly(
            string fileName,
            IVsUpgradeLogger? logger,
            out uint upgradeRequired,
            out Guid migratedProjectFactory,
            out uint upgradeProjectCapabilityFlags)
        {
            // Xproj is deprecated. It cannot be upgraded, and cannot be loaded.
            upgradeRequired = (uint)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED;
            migratedProjectFactory = GetType().GUID;
            upgradeProjectCapabilityFlags = 0;

            // Log a message explaining that the project cannot be automatically upgraded
            // and how to perform the upgrade manually. This message will be presented in
            // the upgrade report.
            logger?.LogMessage(
                (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Path.GetFileNameWithoutExtension(fileName),
                fileName,
                VSResources.XprojNotSupported);
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            // Should not be called
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<IDisposable?> InitializeAsync(ManagedProjectSystemPackage package, IComponentModel componentModel)
        {
            ((IVsProjectFactory)this).SetSite(new ServiceProviderToOleServiceProviderAdapter(ServiceProvider.GlobalProvider));

            package.RegisterProjectFactory(this);

            return Task.FromResult<IDisposable?>(null);
        }
    }
}
