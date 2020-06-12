// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public class DependencyTests
    {
        [Fact]
        public void Dependency_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("dependencyModel", () =>
            {
                new Dependency(null!);
            });

            Assert.Throws<ArgumentNullException>("ProviderType", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = null!, Id = "Id" };
                new Dependency(dependencyModel);
            });

            Assert.Throws<ArgumentNullException>("Id", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = null! };
                new Dependency(dependencyModel);
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

            var dependency = new Dependency(mockModel);

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
                Implicit = true,
                Visible = true,
                Flags = DependencyTreeFlags.GenericDependency,
                Icon = KnownMonikers.Path,
                ExpandedIcon = KnownMonikers.PathIcon,
                UnresolvedIcon = KnownMonikers.PathListBox,
                UnresolvedExpandedIcon = KnownMonikers.PathListBoxItem,
                Properties = new Dictionary<string, string> { { "prop1", "val1" } }.ToImmutableDictionary()
            };

            var dependency = new Dependency(mockModel);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(mockModel.Name, dependency.Name);
            Assert.Equal(mockModel.Caption, dependency.Caption);
            Assert.Equal(mockModel.OriginalItemSpec, dependency.OriginalItemSpec);
            Assert.Equal(mockModel.Path, dependency.Path);
            Assert.Equal(mockModel.SchemaName, dependency.SchemaName);
            Assert.Equal(mockModel.SchemaItemType, dependency.SchemaItemType);
            Assert.Equal(mockModel.Resolved, dependency.Resolved);
            Assert.Equal(mockModel.Implicit, dependency.Implicit);
            Assert.Equal(mockModel.Visible, dependency.Visible);
            Assert.Single(dependency.BrowseObjectProperties);
            Assert.True(dependency.BrowseObjectProperties.ContainsKey("prop1"));
            Assert.Equal(DependencyTreeFlags.Resolved + DependencyTreeFlags.GenericDependency, dependency.Flags);
        }

        [Theory]
        [InlineData(@"../../somepath", @"xxx\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"xxx\__\somepath__")]
        [InlineData(@"somepath", @"xxx\somepath")]
        public void Dependency_Id_NoSnapshotTargetFramework(string modelId, string expectedId)
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "xxx", Id = modelId };

            var dependency = new Dependency(dependencyModel);

            Assert.Equal(dependencyModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Theory]
        [InlineData(@"../../somepath", @"providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"providerType\__\somepath__")]
        [InlineData(@"somepath", @"providerType\somepath")]
        public void Dependency_Id(string modelId, string expectedId)
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = modelId };

            var dependency = new Dependency(dependencyModel);

            Assert.Equal(dependencyModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Fact]
        public void Dependency_SetProperties()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = new Dependency(dependencyModel);
            var flags = ProjectTreeFlags.Create("TestFlag");

            var newDependency = dependency.SetProperties(
                caption: "newcaption",
                resolved: true,
                flags: flags);

            Assert.Equal("newcaption", newDependency.Caption);
            Assert.True(newDependency.Resolved);
            Assert.True(newDependency.Flags.Equals(flags));
        }

        [Fact]
        public void WhenSettingProperties_ExistingIconSetInstanceIsReused()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = (new Dependency(dependencyModel))
                .SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependencyWithUpdatedIconSet = dependency.SetProperties(iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            Assert.Same(dependency.IconSet, dependencyWithUpdatedIconSet.IconSet);
        }

        [Fact]
        public void WhenCreatingADependencyFromAnotherDependency_ExistingIconSetInstanceIsReused()
        {
            var dependencyModel = new TestableDependencyModel(
                    "Path",
                    "ItemSpec",
                    iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependency = new Dependency(dependencyModel);

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

            var dependency1 = new Dependency(model1);
            var dependency2 = new Dependency(model2);

            Assert.Same(dependency1.IconSet, dependency2.IconSet);
        }

        [Fact]
        public void GetID_ThrowsForInvalidArguments()
        {
            var type = "providerType";
            var modelId = "modelId";

            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(null!, modelId));
            Assert.Throws<ArgumentNullException>(() => Dependency.GetID(type, null!));

            Assert.Throws<ArgumentException>(() => Dependency.GetID("", modelId));
            Assert.Throws<ArgumentException>(() => Dependency.GetID(type, ""));
        }

        [Theory]
        [InlineData(@"../../somepath", @"providerType\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"providerType\__\somepath__")]
        [InlineData(@"somepath", @"providerType\somepath")]
        public void GetID_CreatesCorrectString(string modelId, string expected)
        {
            Assert.Equal(expected, Dependency.GetID("providerType", modelId));
        }

        [Fact]
        public void IdEquals()
        {
            Assert.True(Dependency.IdEquals(@"providerType\modelId", "providerType", "modelId"));
            Assert.True(Dependency.IdEquals(@"providerType\modelId", "providerType", "modelId/"));
            Assert.True(Dependency.IdEquals(@"providerType\__\__\modelId", "providerType", "../../modelId"));
            Assert.True(Dependency.IdEquals(@"providerType\__\__\modelId", "providerType", "..\\..\\modelId"));
            Assert.True(Dependency.IdEquals(@"providerType\__\__\modelId", "providerType", "../../modelId/"));
            Assert.True(Dependency.IdEquals(@"PROVIDERTYPE\MODELID", "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"providerType/modelId", "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"providerType/modelId/", "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"XXX\providerType\modelId", "providerType", "modelId"));
            Assert.False(Dependency.IdEquals(@"XXX\modelId", "providerType", "modelId"));
        }
    }
}
