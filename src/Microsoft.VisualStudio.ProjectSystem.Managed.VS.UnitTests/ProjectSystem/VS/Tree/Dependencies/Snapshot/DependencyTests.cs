// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class DependencyTests
    {
        [Fact]
        public void Dependency_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("dependencyModel", () =>
            {
                new Dependency(null, null, null);
            });

            Assert.Throws<ArgumentNullException>("ProviderType", () =>
            {
                var mockModel = IDependencyModelFactory.Create();
                new Dependency(mockModel, null, null);
            });

            Assert.Throws<ArgumentNullException>("Id", () =>
            {
                var mockModel = IDependencyModelFactory.Implement(providerType: "someprovider");
                new Dependency(mockModel, null, null);
            });

            Assert.Throws<ArgumentNullException>("targetFramework", () =>
            {
                var mockModel = IDependencyModelFactory.Implement(providerType: "someprovider", id: "id");
                new Dependency(mockModel, null, null);
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

            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

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
                properties: new Dictionary<string, string>() { { "prop1", "val1" } },
                dependenciesIds: new[] { "otherid" });

            var targetFramework = ITargetFrameworkFactory.Implement("Tfm1");

            var dependency = new Dependency(mockModel, targetFramework, @"C:\Foo\Project.csproj");

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
            Assert.Equal("Tfm1\\xxx\\otherid", dependency.DependencyIDs[0]);
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.ResolvedFlags));
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.DependencyFlags));
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm1\xxx\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm1\xxx\__\somepath__")]
        [InlineData(@"somepath", @"tfm1\xxx\somepath")]
        public void Dependency_Id_NoSnapsotTargetFramework(string modelId, string expectedId)
        {
            var mockModel = IDependencyModelFactory.Implement(providerType: "xxx", id: modelId);
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Create();

            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.cspoj");

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm\providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm\providerType\__\somepath__")]
        [InlineData(@"somepath", @"tfm\providerType\somepath")]
        public void Dependency_Id(string modelId, string expectedId)
        {
            var mockModel = IDependencyModelFactory.Implement(providerType: "providerType", id: modelId);
            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = new Dependency(mockModel, mockTargetFramework, @"C:\Foo\Project.csproj");

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
                caption: caption);
            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = new Dependency(mockModel, mockTargetFramework, @"C:\Foo\Project.csproj");

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

            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");
            var dependency1 = new Dependency(mockModel1, targetFramework, @"C:\Foo\Project.csproj");
            var dependency2 = new Dependency(mockModel2, targetFramework, @"C:\Foo\Project.csproj");
            var dependency3 = new Dependency(mockModel3, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Equal(dependency1, dependency2);
            Assert.NotEqual(dependency1, dependency3);
            Assert.Equal(dependency1.GetHashCode(), dependency2.GetHashCode());
            Assert.NotEqual(dependency1.GetHashCode(), dependency3.GetHashCode());
        }

        [Fact]
        public void Dependency_SetProperties()
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId");

            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

            var newDependency = dependency.SetProperties(
                caption: "newcaption",
                resolved: true,
                flags: DependencyTreeFlags.BaseReferenceFlags,
                dependencyIDs: ImmutableList<string>.Empty.Add("aaa"));

            Assert.Equal("newcaption", newDependency.Caption);
            Assert.True(newDependency.Resolved);
            Assert.True(newDependency.Flags.Equals(DependencyTreeFlags.BaseReferenceFlags));
            Assert.True(newDependency.DependencyIDs.Count == 1);
            Assert.Equal("aaa", newDependency.DependencyIDs[0]);
        }

        [Fact]
        public void Dependency_SetProperties_PreservesDependencyIDs()
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "cube",
                dependencyIDs: ImmutableList<string>.Empty.Add("glass"));
            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

            var expectedId = "tfm1\\providerType\\cube";
            var expectedDependencyId = "tfm1\\providerType\\glass";

            Assert.Equal(expectedId, dependency.Id);
            Assert.True(dependency.DependencyIDs.Count == 1);
            Assert.Equal(expectedDependencyId, dependency.DependencyIDs[0]);

            var newDependency = dependency.SetProperties(
                caption: "newcaption");

            Assert.Equal("newcaption", newDependency.Caption);

            Assert.Equal(expectedId, newDependency.Id);
            Assert.True(newDependency.DependencyIDs.Count == 1);
            Assert.Equal(expectedDependencyId, newDependency.DependencyIDs[0]);
        }

        //[Fact]
        //public void Dependency_DependenciesProperty()
        //{
        //    var mockModel1 = IDependencyModelFactory.Implement(
        //        providerType: "providerType",
        //        id: "someId1",
        //        dependencyIDs: new[] { "someId2", "someId_other" });

        //    var mockModel2 = IDependencyModelFactory.Implement(
        //        providerType: "providerType",
        //        id: "someId2");
        //    var mockModel3 = IDependencyModelFactory.Implement(
        //        providerType: "providerType",
        //        id: "someId_other");

        //    var targetFramework = ITargetFrameworkFactory.Implement("Tfm1");
        //    var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementMock(targetFramework: targetFramework);

        //    var dependency1 = new Dependency(mockModel1, targetFramework);
        //    var dependency2 = new Dependency(mockModel2, targetFramework);
        //    var dependency3 = new Dependency(mockModel3, targetFramework);

        //    var children = new Dictionary<string, IDependency>()
        //    {
        //        { dependency2.Id, dependency2 },
        //        { dependency3.Id, dependency3 },
        //    };

        //    mockSnapshot.Setup(x => x.DependenciesWorld).Returns(
        //        ImmutableDictionary<string, IDependency>.Empty.AddRange(children));

        //    Assert.True(dependency1.Dependencies.Count() == 2);
        //    Assert.True(dependency1.Dependencies.Any(x => x.Equals(dependency2)));
        //    Assert.True(dependency1.Dependencies.Any(x => x.Equals(dependency3)));
        //}

        [Fact]
        public void Dependency_HasUnresolvedDependency()
        {
            var mockModel1 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1",
                resolved: true);

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency(@"tfm1\providerType\someid1", true);

            var dependency = new Dependency(mockModel1, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

            Assert.True(dependency.HasUnresolvedDependency(mockSnapshot));
            Assert.True(dependency.IsOrHasUnresolvedDependency(mockSnapshot));
        }
    }
}
