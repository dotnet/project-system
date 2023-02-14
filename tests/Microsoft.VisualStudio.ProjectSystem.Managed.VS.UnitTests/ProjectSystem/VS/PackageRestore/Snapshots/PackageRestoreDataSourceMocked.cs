// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.Snapshots
{
    internal class PackageRestoreDataSourceMocked : PackageRestoreDataSource
    {
        public PackageRestoreDataSourceMocked(
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            ITelemetryService telemetryService,
            INonModalNotificationService nonModelNotificationService,
            UnconfiguredProject project, 
            IPackageRestoreUnconfiguredInputDataSource dataSource, 
            IProjectAsynchronousTasksService projectAsynchronousTasksService, 
            IFileSystem fileSystem, 
            IManagedProjectDiagnosticOutputService logger, 
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection,
            INuGetRestoreService nuGetRestoreService)
            : base(featureFlagsService, telemetryService, nonModelNotificationService, project, dataSource, projectAsynchronousTasksService, fileSystem, logger, sharedJoinableTaskCollection, nuGetRestoreService)
        {
        }

        protected override bool IsProjectConfigurationVersionOutOfDate(IReadOnlyCollection<PackageRestoreConfiguredInput>? configuredInputs)
        {
            return false;
        }
    }
}
