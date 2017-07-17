// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class PackageDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new PackageDependencyModel(
                providerType: "myProvider",
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                flags: flag,
                version:"myVersion",
                resolved:true,
                isImplicit: false,
                properties: properties,
                isTopLevel: true,
                isVisible:true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myVersion", model.Version);
            Assert.Equal("myPath (myVersion)", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.PackageNodePriority, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(2, model.DependencyIDs.Count);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.PackageNodeFlags));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new PackageDependencyModel(
                providerType: "myProvider",
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                flags: flag,
                version: "myVersion",
                resolved: false,
                isImplicit: false,
                properties: properties,
                isTopLevel: true,
                isVisible: true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myVersion", model.Version);
            Assert.Equal("myPath (myVersion)", model.Caption);
            Assert.Equal(PackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.UnresolvedReferenceNodePriority, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGrey, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(2, model.DependencyIDs.Count);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.PackageNodeFlags));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new PackageDependencyModel(
                providerType: "myProvider",
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                flags: flag,
                version: "",
                resolved: true,
                isImplicit: true,
                properties: properties,
                isTopLevel: true,
                isVisible: true,
                dependenciesIDs: dependencyIDs);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("", model.Version);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.PackageNodePriority, model.Priority);
            Assert.Equal(ManagedImageMonikers.NuGetGreyPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.NuGetGreyWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(2, model.DependencyIDs.Count);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.PackageNodeFlags));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
