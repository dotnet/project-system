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
            var targetFramework = new TargetFramework("tfm");

            Assert.Throws<ArgumentNullException>("dependencyModel", () =>
            {
                new Dependency(null!, targetFramework);
            });

            Assert.Throws<ArgumentNullException>("ProviderType", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = null!, Id = "Id" };
                new Dependency(dependencyModel, targetFramework);
            });

            Assert.Throws<ArgumentNullException>("Id", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = null! };
                new Dependency(dependencyModel, targetFramework);
            });

            Assert.Throws<ArgumentNullException>("targetFramework", () =>
            {
                var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "id" };
                new Dependency(dependencyModel, null!);
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

            var dependency = new Dependency(mockModel, new TargetFramework("tfm1"));

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

            var targetFramework = new TargetFramework("Tfm1");

            var dependency = new Dependency(mockModel, targetFramework);

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
        [InlineData(@"../../somepath", @"tfm1\xxx\__\__\somepath")]
        [InlineData(@"__\somepath..\", @"tfm1\xxx\__\somepath__")]
        [InlineData(@"somepath", @"tfm1\xxx\somepath")]
        public void Dependency_Id_NoSnapshotTargetFramework(string modelId, string expectedId)
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "xxx", Id = modelId };

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm1"));

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

            var dependency = new Dependency(dependencyModel, targetFramework);

            Assert.Equal(dependencyModel.ProviderType, dependency.ProviderType);
            Assert.Equal(expectedId, dependency.Id);
        }

        [Fact]
        public void Dependency_SetProperties()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm1"));
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

            var dependency = (new Dependency(dependencyModel, new TargetFramework("tfm1")))
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

            var dependency = new Dependency(dependencyModel, new TargetFramework("tfm2"));

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

            var dependency1 = new Dependency(model1, targetFramework);
            var dependency2 = new Dependency(model2, targetFramework);

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
