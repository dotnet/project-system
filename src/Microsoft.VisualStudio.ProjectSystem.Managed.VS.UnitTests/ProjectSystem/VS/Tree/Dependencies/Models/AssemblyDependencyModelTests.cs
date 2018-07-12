// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class AssemblyDependencyModelTests
    {
        [Fact]
        public void Resolved_NoFusionName()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AssemblyDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath", model.Caption);
            Assert.Equal(ResolvedAssemblyReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.FrameworkAssemblyNodePriority, model.Priority);
            Assert.Equal(AssemblyReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Reference, model.Icon);
            Assert.Equal(KnownMonikers.Reference, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Resolved_WithFusionName()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add(ResolvedAssemblyReference.FusionNameProperty, "myAssembly.dll");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AssemblyDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myAssembly.dll", model.Caption);
            Assert.Equal(ResolvedAssemblyReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.FrameworkAssemblyNodePriority, model.Priority);
            Assert.Equal(AssemblyReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Reference, model.Icon);
            Assert.Equal(KnownMonikers.Reference, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AssemblyDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath", model.Caption);
            Assert.Equal(AssemblyReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.FrameworkAssemblyNodePriority, model.Priority);
            Assert.Equal(AssemblyReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Reference, model.Icon);
            Assert.Equal(KnownMonikers.Reference, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AssemblyDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath", model.Caption);
            Assert.Equal(ResolvedAssemblyReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.FrameworkAssemblyNodePriority, model.Priority);
            Assert.Equal(AssemblyReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.ReferencePrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.ReferencePrivate, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ReferenceWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
