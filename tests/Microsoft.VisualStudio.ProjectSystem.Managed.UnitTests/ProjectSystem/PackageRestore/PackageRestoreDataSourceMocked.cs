// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal class PackageRestoreDataSourceMocked : PackageRestoreDataSource
    {
        public PackageRestoreDataSourceMocked(
            UnconfiguredProject project,
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection,
            IPackageRestoreUnconfiguredInputDataSource dataSource,
            IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IFileSystem fileSystem,
            IManagedProjectDiagnosticOutputService logger,
            INuGetRestoreService nuGetRestoreService,
            IPackageRestoreCycleDetector cycleDetector)
            : base(project, sharedJoinableTaskCollection, dataSource, projectAsynchronousTasksService, fileSystem, logger, nuGetRestoreService, cycleDetector)
        {
        }

        protected override bool IsRestoreDataVersionOutOfDate(IImmutableDictionary<NamedIdentity, IComparable> dataVersions)
        {
            return false;
        }

        protected override bool IsProjectConfigurationVersionOutOfDate(IReadOnlyCollection<PackageRestoreConfiguredInput>? configuredInputs)
        {
            return false;
        }
    }
}
