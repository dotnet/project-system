// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageDependencyModel(
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                version: "myVersion",
                isResolved: true,
                isImplicit: false,
                properties: properties,
                isTopLevel: true,
                isVisible: true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myPath (myVersion)", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.Package, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.NuGetPackageDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageDependencyModel(
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                version: "myVersion",
                isResolved: false,
                isImplicit: false,
                properties: properties,
                isTopLevel: true,
                isVisible: true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myPath (myVersion)", model.Caption);
            Assert.Equal(PackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.UnresolvedReference, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.NuGetPackageDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageDependencyModel(
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                version: "",
                isResolved: true,
                isImplicit: true,
                properties: properties,
                isTopLevel: true,
                isVisible: true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.Package, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGreyPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.NuGetPackageDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
