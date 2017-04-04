// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependencyTests
    {
        [Fact]
        public void Dependency_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("dependencyModel", () =>
            {
                new Dependency(null, null);
            });

            Assert.Throws<ArgumentNullException>("snapshot", () =>
            {
                var mockModel = IDependencyModelFactory.Create();
                new Dependency(mockModel, null);
            });

            Assert.Throws<ArgumentNullException>("ProviderType", () =>
            {
                var mockModel = IDependencyModelFactory.Create();
                var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();
                new Dependency(mockModel, mockSnapshot);
            });

            Assert.Throws<ArgumentNullException>("Id", () =>
            {
                var mockModel = IDependencyModelFactory.Implement(providerType:"someprovider");
                var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();
                new Dependency(mockModel, mockSnapshot);
            });
        }

        [Fact]
        public void Dependency_Constructor_WhenOptionalValuesNotProvided_ShouldSetDefaults()
        {
            const string jsonModel = @"
{
    ""ProviderType"": ""xxx"",
    ""Id"": ""mymodel""
}";
            var mockModel = IDependencyModelFactory.FromJson(jsonModel);
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();

            var dependency = new Dependency(mockModel, mockSnapshot);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(string.Empty, dependency.Name);
            Assert.Equal(string.Empty, dependency.Version);
            Assert.Equal(string.Empty, dependency.Caption);
            Assert.Equal(string.Empty, dependency.OriginalItemSpec);
            Assert.Equal(string.Empty, dependency.Path);
            Assert.Equal("Folder", dependency.SchemaName);
            Assert.Equal("Folder", dependency.SchemaItemType);
            Assert.True(dependency.Properties.Count == 2);
            Assert.True(dependency.Properties.ContainsKey("Identity"));
            Assert.True(dependency.Properties.ContainsKey("FullPath"));
            Assert.True(dependency.DependencyIDs.Count == 0);
        }

        [Fact]
        public void Dependency_Constructor_WhenValidModelProvided_ShouldSetAllProperties()
        {
            const string jsonModel = @"
{
    ""ProviderType"": ""xxx"",
    ""Id"": ""mymodelid"",
    ""Name"": ""mymodelname"",
    ""Version"": ""2.0.0-1"",
    ""Caption"": ""mymodel"",
    ""OriginalItemSpec"": ""mymodeloriginal"",
    ""Path"": ""mymodelpath"",
    ""SchemaName"": ""MySchema"",
    ""SchemaItemType"": ""MySchemaItemType"",
    ""Resolved"": ""true"",
    ""TopLevel"": ""true"",
    ""Implicit"": ""true"",
    ""Visible"": ""true"",
    ""Priority"": ""3""
}";

            var mockModel = IDependencyModelFactory.FromJson(
                jsonModel,
                flags: DependencyTreeFlags.DependencyFlags.Union(DependencyTreeFlags.GenericDependencyFlags),
                icon: KnownMonikers.Path,
                expandedIcon: KnownMonikers.PathIcon,
                unresolvedIcon: KnownMonikers.PathListBox,
                unresolvedExpandedIcon: KnownMonikers.PathListBoxItem,
                properties: new Dictionary<string, string>() { { "prop1","val1" } },
                dependenciesIds: new[] { "otherid"});

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();

            var dependency = new Dependency(mockModel, mockSnapshot);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(mockModel.Name, dependency.Name);
            Assert.Equal(mockModel.Version, dependency.Version);
            Assert.Equal(mockModel.Caption, dependency.Caption);
            Assert.Equal(mockModel.OriginalItemSpec, dependency.OriginalItemSpec);
            Assert.Equal(mockModel.Path, dependency.Path);
            Assert.Equal(mockModel.SchemaName, dependency.SchemaName);
            Assert.Equal(mockModel.SchemaItemType, dependency.SchemaItemType);
            Assert.Equal(mockModel.Resolved, dependency.Resolved);
            Assert.Equal(mockModel.TopLevel, dependency.TopLevel);
            Assert.Equal(mockModel.Implicit, dependency.Implicit);
            Assert.Equal(mockModel.Visible, dependency.Visible);
            Assert.Equal(mockModel.Priority, dependency.Priority);
            Assert.True(dependency.Properties.Count == 1);
            Assert.True(dependency.Properties.ContainsKey("prop1"));
            Assert.True(dependency.DependencyIDs.Count == 1);
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.ResolvedFlags));
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.DependencyFlags));
        }

        [Theory]
        [InlineData(@"../../somepath", @"__\__\somepath")]
        [InlineData(@"__\somepath..\", @"__\somepath__\")]
        [InlineData(@"somepath", @"somepath")]
        public void Dependency_Id_NoSnapsotTargetFramework(string modelId, string expectedId)
        {
            var mockModel = IDependencyModelFactory.Implement(providerType:"xxx", id: modelId);
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();

            var dependency = new Dependency(mockModel, mockSnapshot);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm\providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm\providerType\__\somepath__\")]
        [InlineData(@"somepath", @"tfm\providerType\somepath")]
        public void Dependency_Id(string modelId, string expectedId)
        {
            var mockModel = IDependencyModelFactory.Implement(providerType: "providerType", id: modelId);
            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker:"tfm");
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Implement(targetFramework: mockTargetFramework);

            var dependency = new Dependency(mockModel, mockSnapshot);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Theory]
        [InlineData(@"SomeDependency", @"", @"SomeDependency", @"SomeDependency")]
        [InlineData(null, @"SomeDependency", @"SomeDependency", @"SomeDependency")]
        [InlineData(null, null, @"SomeDependency", @"SomeDependency")]
        [InlineData(@"SomeOriginalItemSpec", @"", @"SomeDependency", @"SomeDependency (SomeOriginalItemSpec)")]
        public void Dependency_Alias(string originalItemSpec, string path, string caption, string expectedAlias)
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType", 
                id: "someId",
                originalItemSpec: originalItemSpec,
                path: path,
                caption:caption);
            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Implement(targetFramework: mockTargetFramework);

            var dependency = new Dependency(mockModel, mockSnapshot);

            Assert.Equal(expectedAlias, dependency.Alias);
        }

        [Fact]
        public void Dependency_EqualsAndGetHashCode()
        {
            var mockModel1 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1");
            var mockModel2 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1");
            var mockModel3 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId_other");

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Implement();

            var dependency1 = new Dependency(mockModel1, mockSnapshot);
            var dependency2 = new Dependency(mockModel2, mockSnapshot);
            var dependency3 = new Dependency(mockModel3, mockSnapshot);

            Assert.Equal(dependency1, dependency2);
            Assert.NotEqual(dependency1, dependency3);
            Assert.Equal("someid1".GetHashCode(), dependency1.GetHashCode());
        }

        [Fact]
        public void Dependency_SetProperties()
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId");
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Implement();

            var dependency = new Dependency(mockModel, mockSnapshot);

            var newDependency = dependency.SetProperties(
                caption: "newcaption",
                resolved: true,
                flags: DependencyTreeFlags.BaseReferenceFlags,
                dependencyIDs: ImmutableList<string>.Empty.Add("aaa"));

            Assert.Equal("newcaption", newDependency.Caption);
            Assert.Equal(true, newDependency.Resolved);
            Assert.True(newDependency.Flags.Equals(DependencyTreeFlags.BaseReferenceFlags));
            Assert.True(newDependency.DependencyIDs.Count == 1);
        }

        [Fact]
        public void Dependency_DependenciesProperty()
        {
            var mockModel1 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1", 
                dependencyIDs: new[] { "someId2", "someId_other" });

            var mockModel2 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId2");
            var mockModel3 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId_other");

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementMock();

            var dependency1 = new Dependency(mockModel1, mockSnapshot.Object);
            var dependency2 = new Dependency(mockModel2, mockSnapshot.Object);
            var dependency3 = new Dependency(mockModel3, mockSnapshot.Object);

            var children = new Dictionary<string, IDependency>()
            {
                { dependency2.Id, dependency2 },
                { dependency3.Id, dependency3 },
            };

            mockSnapshot.Setup(x => x.DependenciesWorld).Returns(
                ImmutableDictionary<string, IDependency>.Empty.AddRange(children));

            Assert.True(dependency1.Dependencies.Count() == 2);
            Assert.True(dependency1.Dependencies.Any(x => x.Equals(dependency2)));
            Assert.True(dependency1.Dependencies.Any(x => x.Equals(dependency3)));
        }

        [Fact]
        public void Dependency_HasUnresolvedDependency()
        {
            var mockModel1 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1",
                resolved: true);

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency("someId1", true);

            var dependency = new Dependency(mockModel1, mockSnapshot);

            Assert.True(dependency.HasUnresolvedDependency);
            Assert.True(dependency.IsOrHasUnresolvedDependency());
        }
    }
}
