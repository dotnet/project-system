// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;
using Microsoft.VisualStudio.Telemetry;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.Snapshots
{
    internal class PackageRestoreDataSourceMocked : PackageRestoreDataSource
    {
        public PackageRestoreDataSourceMocked(
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            ITelemetryService telemetryService,
            IInfoBarService infoBarService,
            UnconfiguredProject project, 
            IPackageRestoreUnconfiguredInputDataSource dataSource, 
            IProjectAsynchronousTasksService projectAsynchronousTasksService, 
            IVsSolutionRestoreService3 solutionRestoreService, 
            IFileSystem fileSystem, 
            IManagedProjectDiagnosticOutputService logger, 
            IVsSolutionRestoreService4 solutionRestoreService4, 
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection) 
            : base(featureFlagsService, telemetryService, infoBarService, project, dataSource, projectAsynchronousTasksService, solutionRestoreService, fileSystem, logger, solutionRestoreService4, sharedJoinableTaskCollection)
        {
        }

        protected override bool IsProjectConfigurationVersionOutOfDate(System.Collections.Generic.IReadOnlyCollection<PackageRestoreConfiguredInput>? configuredInputs)
        {
            return false;
        }

        protected override bool IsSavedNominationOutOfDate(ConfiguredProject activeConfiguredProject)
        {
            return false;
        }
    }
}
