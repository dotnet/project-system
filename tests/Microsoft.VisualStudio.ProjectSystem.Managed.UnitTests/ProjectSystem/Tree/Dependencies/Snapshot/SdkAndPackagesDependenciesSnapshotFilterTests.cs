// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class SdkAndPackagesDependenciesSnapshotFilterTests : DependenciesSnapshotFilterTestsBase
    {
        private protected override IDependenciesSnapshotFilter CreateFilter() => new SdkAndPackagesDependenciesSnapshotFilter();

        [Fact]
        public void BeforeAddOrUpdate_WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            var targetFramework = new TargetFramework("tfm");

            const string sdkName = "sdkName";

            var sdkDependency = new TestDependency
            {
                Id = "dependency1Id",
                Name = sdkName,
                Resolved = false,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, sdkName),
                Resolved = true,
                Flags = DependencyTreeFlags.PackageDependency
            };

            var builder = new IDependency[] { sdkDependency, packageDependency }.ToDictionary(d => d.Id);

            var context = new AddDependencyContext(builder);

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
                    schemaName: ResolvedSdkReference.SchemaName), acceptedDependency!);

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
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, sdkName),
                Resolved = false,
                Flags = DependencyTreeFlags.PackageDependency
            };

            var builder = new IDependency[] { sdkDependency, packageDependency }.ToDictionary(d => d.Id);

            var context = new AddDependencyContext(builder);

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
            var targetFramework = new TargetFramework("tfm");

            const string packageName = "packageName";

            var sdkDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, packageName),
                Resolved = true,
                Flags = DependencyTreeFlags.PackageDependency.Union(DependencyTreeFlags.Unresolved) // to see if unresolved is fixed
            };

            var packageDependency = new TestDependency
            {
                Id = "packageId",
                Name = packageName,
                Flags = DependencyTreeFlags.PackageDependency,
                Resolved = true
            };

            var builder = new IDependency[] { packageDependency, sdkDependency }.ToDictionary(d => d.Id);

            var context = new AddDependencyContext(builder);

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
                    schemaName: ResolvedSdkReference.SchemaName), sdkDependencyAfter);
        }

        [Fact]
        public void BeforeRemove_WhenPackageRemoving_ShouldCleanupSdk()
        {
            const string packageName = "packageName";

            var targetFramework = new TargetFramework("tfm");

            var sdkDependency = new TestDependency
            {
                Id = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, packageName),
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency.Union(DependencyTreeFlags.Resolved)
            };

            var packageDependency = new TestDependency
            {
                Id = "packageId",
                Name = packageName,
                Flags = DependencyTreeFlags.PackageDependency,
                Resolved = true
            };

            var builder = new IDependency[] { packageDependency, sdkDependency }.ToDictionary(d => d.Id);

            var context = new RemoveDependencyContext(builder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                targetFramework: targetFramework,
                dependency: packageDependency,
                context);

            // Accepts removal
            Assert.True(context.GetResult(filter));

            // Makes other changes too
            Assert.True(context.Changed);

            Assert.True(builder.TryGetValue(packageDependency.Id, out var afterPackageDependency));
            Assert.Same(packageDependency, afterPackageDependency);

            Assert.True(builder.TryGetValue(sdkDependency.Id, out var afterSdkDependency));
            DependencyAssert.Equal(
                afterSdkDependency.ToUnresolved(
                    SdkReference.SchemaName), afterSdkDependency);
        }
    }
}
