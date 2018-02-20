// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class SdkDependencyModelTests
    {
        [Fact]
        public void SdkDependencyModelTests_Resolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("Version", "2.0.0");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SdkDependencyModel(
                "myProvider",
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal("2.0.0", model.Version);
            Assert.Equal(ResolvedSdkReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Sdk, model.Icon);
            Assert.Equal(ManagedImageMonikers.Sdk, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void SdkDependencyModelTests_Unresolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("Version", "2.0.0");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SdkDependencyModel(
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
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal("2.0.0", model.Version);
            Assert.Equal(SdkReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Sdk, model.Icon);
            Assert.Equal(ManagedImageMonikers.Sdk, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void SdkDependencyModelTests_Implicit()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("Version", "2.0.0");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new SdkDependencyModel(
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
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal("2.0.0", model.Version);
            Assert.Equal(ResolvedSdkReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.SdkPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.SdkPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
