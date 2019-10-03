// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public sealed class SdkAndPackagesDependenciesSnapshotFilterTests : DependenciesSnapshotFilterTestsBase
    {
        private protected override IDependenciesSnapshotFilter CreateFilter() => new SdkAndPackagesDependenciesSnapshotFilter();

        [Fact]
        public void BeforeAddOrUpdate_WhenNotTopLevelOrResolved_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(new TestDependency
            {
                Id = "dependency1",
                TopLevel = false,
                Flags = DependencyTreeFlags.SdkDependency
            });
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            var dependencyIDs = ImmutableArray.Create("id1", "id2");

            var targetFramework = new TargetFramework("tfm");

            const string sdkName = "sdkName";

            var sdkDependency = new TestDependency
            {
                Id = "dependency1Id",
                Name = sdkName,
                TopLevel = true,
                Resolved = false,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, sdkName),
                Resolved = true,
                DependencyIDs = dependencyIDs,
                Flags = DependencyTreeFlags.NuGetPackageDependency
            };

            var worldBuilder = new IDependency[] { sdkDependency, packageDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                targetFramework,
                sdkDependency,
                null!,
                null,
                context);

            var acceptedDependency = context.GetResult(filter);

            // Dependency should be accepted, but converted to resolved state
            Assert.NotNull(acceptedDependency);
            Assert.NotSame(sdkDependency, acceptedDependency);
            DependencyAssert.Equal(
                sdkDependency.ToResolved(
                    schemaName: ResolvedSdkReference.SchemaName,
                    dependencyIDs: dependencyIDs), acceptedDependency!);

            // No changes other than the filtered dependency
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenSdkAndPackageUnresolved_ShouldDoNothing()
        {
            var targetFramework = new TargetFramework("tfm");

            const string sdkName = "sdkName";

            var sdkDependency = new TestDependency
            {
                Id = "dependency1",
                Name = sdkName,
                TopLevel = false,
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, sdkName),
                Resolved = false,
                Flags = DependencyTreeFlags.NuGetPackageDependency
            };

            var worldBuilder = new IDependency[] { sdkDependency, packageDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                targetFramework,
                sdkDependency,
                null!,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(sdkDependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenPackage_ShouldFindMatchingSdkAndSetProperties()
        {
            var dependencyIDs = ImmutableArray.Create("id1", "id2");

            var targetFramework = new TargetFramework("tfm");

            const string packageName = "packageName";

            var sdkDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, packageName),
                TopLevel = false,
                Resolved = true,
                Flags = DependencyTreeFlags.NuGetPackageDependency.Union(DependencyTreeFlags.Unresolved) // to see if unresolved is fixed
            };

            var packageDependency = new TestDependency
            {
                Id = "packageId",
                Name = packageName,
                Flags = DependencyTreeFlags.NuGetPackageDependency,
                TopLevel = true,
                Resolved = true,
                DependencyIDs = dependencyIDs
            };

            var worldBuilder = new IDependency[] { packageDependency, sdkDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                targetFramework,
                packageDependency,
                null!,
                null,
                context);

            // Accepts unchanged dependency
            Assert.Same(packageDependency, context.GetResult(filter));

            // Other changes made
            Assert.True(context.Changed);

            Assert.True(context.TryGetDependency(sdkDependency.Id, out IDependency sdkDependencyAfter));
            DependencyAssert.Equal(
                sdkDependency.ToResolved(
                    schemaName: ResolvedSdkReference.SchemaName,
                    dependencyIDs: dependencyIDs), sdkDependencyAfter);
        }

        [Fact]
        public void BeforeRemove_WhenPackageRemoving_ShouldCleanupSdk()
        {
            const string packageName = "packageName";

            var targetFramework = new TargetFramework("tfm");

            var sdkDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, packageName),
                TopLevel = false,
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency.Union(DependencyTreeFlags.Resolved)
            };

            var packageDependency = new TestDependency
            {
                Id = "packageId",
                Name = packageName,
                Flags = DependencyTreeFlags.NuGetPackageDependency,
                TopLevel = true,
                Resolved = true
            };

            var worldBuilder = new IDependency[] { packageDependency, sdkDependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new RemoveDependencyContext(worldBuilder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                targetFramework: targetFramework,
                dependency: packageDependency,
                context);

            // Accepts removal
            Assert.True(context.GetResult(filter));

            // Makes other changes too
            Assert.True(context.Changed);

            Assert.True(worldBuilder.TryGetValue(packageDependency.Id, out var afterPackageDependency));
            Assert.Same(packageDependency, afterPackageDependency);

            Assert.True(worldBuilder.TryGetValue(sdkDependency.Id, out var afterSdkDependency));
            DependencyAssert.Equal(
                afterSdkDependency.ToUnresolved(
                    SdkReference.SchemaName,
                    dependencyIDs: ImmutableArray<string>.Empty), afterSdkDependency);
        }
    }
}
