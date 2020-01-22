// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public class DependencyTests
    {
        [Fact]
        public void Dependency_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            var targetFramework = new TargetFramework("tfm");
            var containingProjectPath = @"c:\SomePath\SomeProject.csproj";

            Assert.Throws<ArgumentNullException>("dependencyModel", () =>
            {
                new Dependency(null!, targetFramework, containingProjectPath);
            });

            Assert.Throws<ArgumentNullException>("ProviderType", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = null!, Id = "Id" };
                new Dependency(dependencyModel, targetFramework, containingProjectPath);
            });

            Assert.Throws<ArgumentNullException>("Id", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = null! };
                new Dependency(dependencyModel, targetFramework, containingProjectPath);
            });

            Assert.Throws<ArgumentNullException>("targetFramework", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "id" };
                new Dependency(dependencyModel, null!, containingProjectPath);
            });

            Assert.Throws<ArgumentNullException>("containingProjectPath", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "id" };
                new Dependency(dependencyModel, targetFramework: targetFramework, containingProjectPath: null!);
            });
        }

        [Fact]
        public void Dependency_Constructor_WhenOptionalValuesNotProvided_ShouldSetDefaults()
        {
            var mockModel = new TestDependencyModel
            {
                ProviderType = "xxx",
                Id = "mymodel"
            };

            var dependency = new Dependency(mockModel, new TargetFramework("tfm1"), @"C:\Foo\Project.csproj");

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(string.Empty, dependency.Name);
            Assert.Equal(string.Empty, dependency.Caption);
            Assert.Equal(string.Empty, dependency.OriginalItemSpec);
            Assert.Equal(string.Empty, dependency.Path);
            Assert.Equal("Folder", dependency.SchemaName);
            Assert.Equal("Folder", dependency.SchemaItemType);
            AssertEx.CollectionLength(dependency.BrowseObjectProperties, 2);
            Assert.True(dependency.BrowseObjectProperties.ContainsKey("Identity"));
            Assert.True(dependency.BrowseObjectProperties.ContainsKey("FullPath"));
            Assert.Empty(dependency.DependencyIDs);
        }

        [Fact]
        public void Dependency_Constructor_WhenValidModelProvided_ShouldSetAllProperties()
        {
            var mockModel = new TestDependencyModel
            {
                ProviderType = "xxx",
                Id = "mymodelid",
                Name = "mymodelname",
                Caption = "mymodel",
                OriginalItemSpec = "mymodeloriginal",
                Path = "mymodelpath",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Resolved = true,
                TopLevel = true,
                Implicit = true,
                Visible = true,
                Priority = 3,
                Flags = DependencyTreeFlags.DependencyFlags.Union(DependencyTreeFlags.GenericDependency),
                Icon = KnownMonikers.Path,
                ExpandedIcon = KnownMonikers.PathIcon,
                UnresolvedIcon = KnownMonikers.PathListBox,
                UnresolvedExpandedIcon = KnownMonikers.PathListBoxItem,
                Properties = new Dictionary<string, string> { { "prop1", "val1" } }.ToImmutableDictionary(),
                DependencyIDs = new[] { "otherid" }.ToImmutableArray()
            };

            var targetFramework = new TargetFramework("Tfm1");

            var dependency = new Dependency(mockModel, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(mockModel.Name, dependency.Name);
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
            Assert.Single(dependency.BrowseObjectProperties);
            Assert.True(dependency.BrowseObjectProperties.ContainsKey("prop1"));
            Assert.Single(dependency.DependencyIDs);
            Assert.Equal("Tfm1\\xxx\\otherid", dependency.DependencyIDs[0]);
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.Resolved));
            Assert.True(dependency.Flags.Contains(DependencyTreeFlags.DependencyFlags));
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm1\xxx\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm1\xxx\__\somepath__")]
        [InlineData(@"somepath", @"tfm1\xxx\somepath")]
        public void Dependency_Id_NoSnapshotTargetFramework(string modelId, string expectedId)
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "xxx", Id = modelId };

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm1"), @"C:\Foo\Project.cspoj");

            Assert.Equal(dependencyModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Theory]
        [InlineData(@"../../somepath", @"tfm\providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm\providerType\__\somepath__")]
        [InlineData(@"somepath", @"tfm\providerType\somepath")]
        public void Dependency_Id(string modelId, string expectedId)
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = modelId };
            var targetFramework = new TargetFramework("tfm");

            var dependency = new Dependency(dependencyModel, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Equal(dependencyModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Fact]
        public void Dependency_SetProperties()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm1"), @"C:\Foo\Project.csproj");

            var newDependency = dependency.SetProperties(
                caption: "newcaption",
                resolved: true,
                flags: DependencyTreeFlags.BaseReferenceFlags,
                dependencyIDs: ImmutableArray.Create("aaa"));

            Assert.Equal("newcaption", newDependency.Caption);
            Assert.True(newDependency.Resolved);
            Assert.True(newDependency.Flags.Equals(DependencyTreeFlags.BaseReferenceFlags));
            Assert.Single(newDependency.DependencyIDs);
            Assert.Equal("aaa", newDependency.DependencyIDs[0]);
        }

        [Fact]
        public void Dependency_SetProperties_PreservesDependencyIDs()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "cube", DependencyIDs = ImmutableList<string>.Empty.Add("glass") };
            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm1"), @"C:\Foo\Project.csproj");

            var expectedId = "tfm1\\providerType\\cube";
            var expectedDependencyId = "tfm1\\providerType\\glass";

            Assert.Equal(expectedId, dependency.Id);
            Assert.Single(dependency.DependencyIDs);
            Assert.Equal(expectedDependencyId, dependency.DependencyIDs[0]);

            var newDependency = dependency.SetProperties(
                caption: "newcaption");

            Assert.Equal("newcaption", newDependency.Caption);

            Assert.Equal(expectedId, newDependency.Id);
            Assert.Single(newDependency.DependencyIDs);
            Assert.Equal(expectedDependencyId, newDependency.DependencyIDs[0]);
        }

        [Fact]
        public void Dependency_DependencyIDsProperty()
        {
            var dependencyModel1 = new TestDependencyModel
            {
                ProviderType = "providerType",
                Id = "someId1",
                DependencyIDs = new[] { "someId2", "someId_other" }.ToImmutableArray()
            };
            var dependencyModel2 = new TestDependencyModel
            {
                ProviderType = "providerType",
                Id = "someId2"
            };
            var dependencyModel3 = new TestDependencyModel
            {
                ProviderType = "providerType",
                Id = "someId_other"
            };

            var targetFramework = new TargetFramework("Tfm1");

            var projectPath = "projectPath";

            var dependency1 = new Dependency(dependencyModel1, targetFramework, projectPath);
            var dependency2 = new Dependency(dependencyModel2, targetFramework, projectPath);
            var dependency3 = new Dependency(dependencyModel3, targetFramework, projectPath);

            AssertEx.CollectionLength(dependency1.DependencyIDs, 2);
            Assert.Contains(dependency1.DependencyIDs, x => x.Equals(dependency2.Id));
            Assert.Contains(dependency1.DependencyIDs, x => x.Equals(dependency3.Id));
        }

        [Fact]
        public void WhenSettingProperties_ExistingIconSetInstanceIsReused()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = (new Dependency(dependencyModel, new TargetFramework("tfm1"), @"C:\Foo\Project.csproj"))
                .SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependencyWithUpdatedIconSet = dependency.SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            Assert.Same(dependency.IconSet, dependencyWithUpdatedIconSet.IconSet);
        }

        [Fact]
        public void WhenCreatingADependencyFromAnotherDependency_ExistingIconSetInstanceIsReused()
        {
            var projectPath = @"C:\Foo\Project.csproj";

            var dependencyModel = new TestableDependencyModel(
                    projectPath,
                    "ItemSpec",
                    iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm2"), projectPath);

            Assert.Same(dependencyModel.IconSet, dependency.IconSet);
        }

        private sealed class TestableDependencyModel : DependencyModel
        {
            public override string ProviderType => "someProvider";

            public override DependencyIconSet IconSet { get; }

            public TestableDependencyModel(string path, string originalItemSpec, DependencyIconSet iconSet)
                : base(path, originalItemSpec, ProjectTreeFlags.Empty, isResolved: false, isImplicit: false, properties: null)
            {
                IconSet = iconSet;
            }
        }

        [Fact]
        public void WhenCreatingUnrelatedDependenciesWithSameIcons_BothUseSameIconSet()
        {
            var model1 = new TestDependencyModel
            {
                ProviderType = "alpha",
                Id = "modelOne",
                Icon = KnownMonikers.Path,
                ExpandedIcon = KnownMonikers.PathIcon,
                UnresolvedIcon = KnownMonikers.PathListBox,
                UnresolvedExpandedIcon = KnownMonikers.PathListBoxItem
            };

            var model2 = new TestDependencyModel
            {
                ProviderType = "beta",
                Id = "modelTwo",
                Icon = KnownMonikers.Path,
                ExpandedIcon = KnownMonikers.PathIcon,
                UnresolvedIcon = KnownMonikers.PathListBox,
                UnresolvedExpandedIcon = KnownMonikers.PathListBoxItem
            };

            var targetFramework = new TargetFramework("Tfm1");

            var dependency1 = new Dependency(model1, targetFramework, @"C:\Foo\Project.csproj");
            var dependency2 = new Dependency(model2, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Same(dependency1.IconSet, dependency2.IconSet);
        }

        [Fact]
        public void GetID_ThrowsForInvalidArguments()
        {
            var tfm = TargetFramework.Any;
            var type = "providerType";
            var modelId = "modelId";

            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(null!, type, modelId));
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(tfm, null!, modelId));
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(tfm, type, null!));

            Assert.Throws<ArgumentException>(() => Dependency.GetID(tfm, "", modelId));
            Assert.Throws<ArgumentException>(() => Dependency.GetID(tfm, type, ""));
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
