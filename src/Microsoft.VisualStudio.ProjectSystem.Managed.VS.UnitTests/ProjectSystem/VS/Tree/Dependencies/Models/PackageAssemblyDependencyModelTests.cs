// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class PackageAssemblyDependencyModelTests
    {
        [Fact]
        public void PackageAssemblyDependencyModelTests_Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new PackageAssemblyDependencyModel(
                providerType: "myProvider",
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                flags: flag,
                resolved: true,
                properties: properties,
                dependenciesIDs: dependencyIDs);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Null(model.SchemaName);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.PackageAssemblyNodePriority, model.Priority);
            Assert.Equal(KnownMonikers.Reference, model.Icon);
            Assert.Equal(KnownMonikers.Reference, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void PackageAssemblyDependencyModelTests_Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new PackageAssemblyDependencyModel(
                providerType: "myProvider",
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                flags: flag,
                resolved: true,
                properties: properties,
                dependenciesIDs: dependencyIDs);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Null(model.SchemaName);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.PackageAssemblyNodePriority, model.Priority);
            Assert.Equal(KnownMonikers.Reference, model.Icon);
            Assert.Equal(KnownMonikers.Reference, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
