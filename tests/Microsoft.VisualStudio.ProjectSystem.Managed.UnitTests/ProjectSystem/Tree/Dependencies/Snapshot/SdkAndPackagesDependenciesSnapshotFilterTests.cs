// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class SdkAndPackagesDependenciesSnapshotFilterTests
    {
        [Fact]
        public void BeforeAddOrUpdate_WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            const string sdkName = "sdkName";

            var sdkDependency = new TestDependency
            {
                Id = sdkName,
                ProviderType = SdkRuleHandler.ProviderTypeString,
                Resolved = false,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = sdkName,
                ProviderType = PackageRuleHandler.ProviderTypeString,
                Resolved = true,
                Flags = DependencyTreeFlags.PackageDependency
            };

            var builder = new IDependency[] { sdkDependency, packageDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(builder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                sdkDependency,
                context);

            var acceptedDependency = context.GetResult(filter);

            // Dependency should be accepted, but converted to resolved state
            Assert.NotNull(acceptedDependency);
            Assert.NotSame(sdkDependency, acceptedDependency);
            DependencyAssert.Equal(
                sdkDependency.ToResolved(schemaName: ResolvedSdkReference.SchemaName, diagnosticLevel: DiagnosticLevel.None),
                acceptedDependency!);

            // No changes other than the filtered dependency
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenSdkAndPackageUnresolved_ShouldDoNothing()
        {
            const string sdkName = "sdkName";

            var sdkDependency = new TestDependency
            {
                Id = "dependency1",
                ProviderType = SdkRuleHandler.ProviderTypeString,
                OriginalItemSpec = sdkName,
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency
            };

            var packageDependency = new TestDependency
            {
                Id = sdkName,
                ProviderType = PackageRuleHandler.ProviderTypeString,
                Resolved = false,
                Flags = DependencyTreeFlags.PackageDependency
            };

            var builder = new IDependency[] { sdkDependency, packageDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(builder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                sdkDependency,
                context);

            // Accepts unchanged dependency
            Assert.Same(sdkDependency, context.GetResult(filter));

            // No other changes made
            Assert.False(context.Changed);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenPackage_ShouldFindMatchingSdkAndSetProperties()
        {
            const string packageName = "packageName";

            var sdkDependency = new TestDependency
            {
                Id = packageName,
                ProviderType = SdkRuleHandler.ProviderTypeString,
                Resolved = true,
                Flags = DependencyTreeFlags.PackageDependency.Union(ProjectTreeFlags.BrokenReference) // to see if unresolved is fixed
            };

            var packageDependency = new TestDependency
            {
                Id = packageName,
                ProviderType = PackageRuleHandler.ProviderTypeString,
                Flags = DependencyTreeFlags.PackageDependency,
                Resolved = true
            };

            var builder = new IDependency[] { packageDependency, sdkDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new AddDependencyContext(builder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeAddOrUpdate(
                packageDependency,
                context);

            // Accepts unchanged dependency
            Assert.Same(packageDependency, context.GetResult(filter));

            // Other changes made
            Assert.True(context.Changed);

            Assert.True(context.TryGetDependency(sdkDependency.GetDependencyId(), out IDependency sdkDependencyAfter));
            DependencyAssert.Equal(
                sdkDependency.ToResolved(schemaName: ResolvedSdkReference.SchemaName, diagnosticLevel: DiagnosticLevel.None),
                sdkDependencyAfter);
        }

        [Fact]
        public void BeforeRemove_WhenPackageRemoving_ShouldCleanupSdk()
        {
            const string packageName = "packageName";

            var sdkDependency = new TestDependency
            {
                Id = packageName,
                ProviderType = SdkRuleHandler.ProviderTypeString,
                Resolved = true,
                Flags = DependencyTreeFlags.SdkDependency.Union(ProjectTreeFlags.ResolvedReference)
            };

            var packageDependency = new TestDependency
            {
                Id = packageName,
                ProviderType = PackageRuleHandler.ProviderTypeString,
                Flags = DependencyTreeFlags.PackageDependency,
                Resolved = true
            };

            var builder = new IDependency[] { packageDependency, sdkDependency }.ToDictionary(IDependencyExtensions.GetDependencyId);

            var context = new RemoveDependencyContext(builder);

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                dependency: packageDependency,
                context);

            // Accepts removal
            Assert.True(context.GetResult(filter));

            // Makes other changes too
            Assert.True(context.Changed);

            Assert.True(builder.TryGetValue(packageDependency.GetDependencyId(), out var afterPackageDependency));
            Assert.Same(packageDependency, afterPackageDependency);

            Assert.True(builder.TryGetValue(sdkDependency.GetDependencyId(), out var afterSdkDependency));
            DependencyAssert.Equal(
                afterSdkDependency.ToUnresolved(SdkReference.SchemaName),
                afterSdkDependency);
        }
    }
}
