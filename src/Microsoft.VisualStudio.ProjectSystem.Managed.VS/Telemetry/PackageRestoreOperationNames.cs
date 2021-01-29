// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal static class PackageRestoreOperationNames
    {
        internal const string BeginNominateRestore = nameof(BeginNominateRestore);
        internal const string EndNominateRestore = nameof(EndNominateRestore);
        internal const string PackageRestoreConfiguredInputDataSourceLinkedToExternalInput = nameof(PackageRestoreConfiguredInputDataSourceLinkedToExternalInput);
        internal const string PackageRestoreDataSourceLinkedToExternalInput= nameof(PackageRestoreDataSourceLinkedToExternalInput);
        internal const string PackageRestoreDataSourceLoading= nameof(PackageRestoreDataSourceLoading);
        internal const string PackageRestoreDataSourceUnloading= nameof(PackageRestoreDataSourceUnloading);
        internal const string PackageRestoreProgressTrackerActivating= nameof(PackageRestoreProgressTrackerActivating);
        internal const string PackageRestoreProgressTrackerDeactivating= nameof(PackageRestoreProgressTrackerDeactivating);
        internal const string PackageRestoreProgressTrackerInstanceInitialized= nameof(PackageRestoreProgressTrackerInstanceInitialized);
        internal const string PackageRestoreProgressTrackerRestoreCompleted= nameof(PackageRestoreProgressTrackerRestoreCompleted);
        internal const string PackageRestoreUnconfiguredInputDataSourceLinkedToExternalInput= nameof(PackageRestoreUnconfiguredInputDataSourceLinkedToExternalInput);
    }
}
