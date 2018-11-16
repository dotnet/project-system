// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
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

            Assert.Throws<ArgumentNullException>("containingProjectPath", () =>
            {
                var mockModel = IDependencyModelFactory.Implement(providerType: "someprovider", id: "id", originalItemSpec: "originalItemSpec");
                new Dependency(mockModel, targetFramework: new TargetFramework("tfm"), containingProjectPath: null);
            });
        }

        [Fact]
        public void Dependency_Constructor_WhenOptionalValuesNotProvided_ShouldSetDefaults()
        {
            var mockModel = IDependencyModelFactory.FromJson(@"
            {
                ""ProviderType"": ""xxx"",
                ""Id"": ""mymodel""
            }");

            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(string.Empty, dependency.Name);
            Assert.Equal(string.Empty, dependency.Version);
            Assert.Equal(string.Empty, dependency.Caption);
            Assert.Equal(string.Empty, dependency.OriginalItemSpec);
            Assert.Equal(string.Empty, dependency.Path);
            Assert.Equal("Folder", dependency.SchemaName);
            Assert.Equal("Folder", dependency.SchemaItemType);
            AssertEx.CollectionLength(dependency.Properties, 2);
            Assert.True(dependency.Properties.ContainsKey("Identity"));
            Assert.True(dependency.Properties.ContainsKey("FullPath"));
            Assert.Empty(dependency.DependencyIDs);
        }

        [Fact]
        public void Dependency_Constructor_WhenValidModelProvided_ShouldSetAllProperties()
        {
            var mockModel = IDependencyModelFactory.FromJson(@"
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
                }",
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
            Assert.Single(dependency.Properties);
            Assert.True(dependency.Properties.ContainsKey("prop1"));
            Assert.Single(dependency.DependencyIDs);
            Assert.Equal("Tfm1\\xxx\\otherid", dependency.DependencyIDs[0]);
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.ResolvedFlags));
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.DependencyFlags));
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm1\xxx\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm1\xxx\__\somepath__")]
        [InlineData(@"somepath", @"tfm1\xxx\somepath")]
        public void Dependency_Id_NoSnapshotTargetFramework(string modelId, string expectedId)
        {
            var mockModel = IDependencyModelFactory.Implement(providerType: "xxx", id: modelId);

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
            Assert.False(dependency1.Equals(other: null));
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
            Assert.Single(newDependency.DependencyIDs);
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
            Assert.Single(dependency.DependencyIDs);
            Assert.Equal(expectedDependencyId, dependency.DependencyIDs[0]);

            var newDependency = dependency.SetProperties(
                caption: "newcaption");

            Assert.Equal("newcaption", newDependency.Caption);

            Assert.Equal(expectedId, newDependency.Id);
            Assert.True(newDependency.DependencyIDs.Count == 1);
            Assert.Equal(expectedDependencyId, newDependency.DependencyIDs[0]);
        }

        [Fact]
        public void Dependency_DependencyIDsProperty()
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

            var targetFramework = ITargetFrameworkFactory.Implement("Tfm1");
            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementMock(targetFramework: targetFramework);

            var projectPath = "projectPath";

            var dependency1 = new Dependency(mockModel1, targetFramework, projectPath);
            var dependency2 = new Dependency(mockModel2, targetFramework, projectPath);
            var dependency3 = new Dependency(mockModel3, targetFramework, projectPath);

            var children = new IDependency[] { dependency1, dependency2, dependency3 }.ToImmutableDictionary(d => d.Id);

            mockSnapshot.Setup(x => x.DependenciesWorld).Returns(children);

            AssertEx.CollectionLength(dependency1.DependencyIDs, 2);
            Assert.Contains(dependency1.DependencyIDs, x => x.Equals(dependency2.Id));
            Assert.Contains(dependency1.DependencyIDs, x => x.Equals(dependency3.Id));
        }

        [Fact]
        public void Dependency_HasUnresolvedDependency()
        {
            var mockModel1 = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId1",
                resolved: true);

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency(@"tfm1\providerType\someid1", hasUnresolvedDependency: true);

            var dependency = new Dependency(mockModel1, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj");

            Assert.True(dependency.HasUnresolvedDependency(mockSnapshot));
            Assert.True(dependency.IsOrHasUnresolvedDependency(mockSnapshot));
        }

        [Fact]
        public void WhenSettingProperties_ExistingIconSetInstanceIsReused()
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId");

            var dependency = (new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj"))
                .SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependencyWithUpdatedIconSet = dependency.SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            Assert.Same(dependency.IconSet, dependencyWithUpdatedIconSet.IconSet);
        }

        [Fact]
        public void WhenCreatingADependencyFromAnotherDependency_ExistingIconSetInstanceIsReused()
        {
            var mockModel = IDependencyModelFactory.Implement(
                providerType: "providerType",
                id: "someId");

            var dependency = new Dependency(mockModel, ITargetFrameworkFactory.Implement("tfm1"), @"C:\Foo\Project.csproj")
                .SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var newDependency = new Dependency(dependency, ITargetFrameworkFactory.Implement("tfm2"), @"C:\Foo\Project.csproj");

            Assert.Same(dependency.IconSet, newDependency.IconSet);
        }

        [Fact]
        public void WhenCreatingUnrelatedDependenciesWithSameIcons_BothUseSameIconSet()
        {
            var model1 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""alpha"",
                    ""Id"": ""modelOne"",
                }",
                icon: KnownMonikers.Path,
                expandedIcon: KnownMonikers.PathIcon,
                unresolvedIcon: KnownMonikers.PathListBox,
                unresolvedExpandedIcon: KnownMonikers.PathListBoxItem);

            var model2 = IDependencyModelFactory.FromJson(@"
                {
                    ""ProviderType"": ""beta"",
                    ""Id"": ""modelTwo"",
                }",
                icon: KnownMonikers.Path,
                expandedIcon: KnownMonikers.PathIcon,
                unresolvedIcon: KnownMonikers.PathListBox,
                unresolvedExpandedIcon: KnownMonikers.PathListBoxItem);

            var targetFramework = ITargetFrameworkFactory.Implement("Tfm1");

            var dependency1 = new Dependency(model1, targetFramework, @"C:\Foo\Project.csproj");
            var dependency2 = new Dependency(model2, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Same(dependency1.IconSet, dependency2.IconSet);
        }

        [Fact]
        public void GetID_ThrowsForInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(null, "providerType", "modelId"));
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(TargetFramework.Any, null, "modelId"));
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(TargetFramework.Any, "providerType", null));

            Assert.Throws<ArgumentException>(() => Dependency.GetID(TargetFramework.Any, "", "modelId"));
            Assert.Throws<ArgumentException>(() => Dependency.GetID(TargetFramework.Any, "providerType", ""));
        }

        [Theory]
        [InlineData(@"../../somepath", @"any\providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"any\providerType\__\somepath__")]
        [InlineData(@"somepath", @"any\providerType\somepath")]
        public void GetID_CreatesCorrectString(string modelId, string expected)
        {
            Assert.Equal(expected, Dependency.GetID(TargetFramework.Any, "providerType", modelId));
        }

        [Fact]
        public void IdEquals()
        {
            Assert.True(Dependency.IdEquals(@"any\providerType\modelId", TargetFramework.Any, "providerType", "modelId"));
            Assert.True(Dependency.IdEquals(@"any\providerType\modelId", TargetFramework.Any, "providerType", "modelId/"));
            Assert.True(Dependency.IdEquals(@"any\providerType\__\__\modelId", TargetFramework.Any, "providerType", "../../modelId"));
            Assert.True(Dependency.IdEquals(@"any\providerType\__\__\modelId", TargetFramework.Any, "providerType", "..\\..\\modelId"));
            Assert.True(Dependency.IdEquals(@"any\providerType\__\__\modelId", TargetFramework.Any, "providerType", "../../modelId/"));
            Assert.True(Dependency.IdEquals(@"ANY\PROVIDERTYPE\MODELID", TargetFramework.Any, "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"any/providerType/modelId", TargetFramework.Any, "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"any/providerType/modelId/", TargetFramework.Any, "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"XXX\providerType\modelId", TargetFramework.Any, "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"any\XXX\modelId", TargetFramework.Any, "providerType", "modelId"));
        }
    }
}
