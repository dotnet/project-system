// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class SharedSharedProjectDependencyModelTests
    {
        [Fact]
        public void SharedSharedProjectDependencyModelTests_Resolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SharedProjectDependencyModel(
                "myProvider",
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                flags:flag,
                resolved:true,
                isImplicit:false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.Equal(true, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.ProjectNodePriority, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SharedProject, model.Icon);
            Assert.Equal(KnownMonikers.SharedProject, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SharedProjectFlags));
            Assert.False(model.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void SharedProjectDependencyModelTests_Unresolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SharedProjectDependencyModel(
                "myProvider",
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                flags: flag,
                resolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ProjectReference.SchemaName, model.SchemaName);
            Assert.Equal(false, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.ProjectNodePriority, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SharedProject, model.Icon);
            Assert.Equal(KnownMonikers.SharedProject, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SharedProjectFlags));
            Assert.False(model.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void SharedProjectDependencyModelTests_Implicit()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SharedProjectDependencyModel(
                "myProvider",
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.Equal(true, model.Resolved);
            Assert.Equal(true, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.ProjectNodePriority, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.SharedProjectPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.SharedProjectPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SharedProjectWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SharedProjectFlags));
            Assert.False(model.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties));
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
