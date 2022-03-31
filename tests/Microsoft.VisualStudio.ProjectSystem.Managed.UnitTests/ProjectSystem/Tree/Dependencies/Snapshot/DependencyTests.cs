// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

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
            Assert.Equal(string.Empty, dependency.Caption);
            Assert.Null(dependency.OriginalItemSpec);
            Assert.Null(dependency.FilePath);
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
                Caption = "mymodel",
                OriginalItemSpec = "mymodeloriginal",
                Path = "mymodelpath",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Resolved = true,
                Implicit = true,
                Visible = true,
                Flags = DependencyTreeFlags.Dependency,
                Icon = KnownMonikers.Path,
                ExpandedIcon = KnownMonikers.PathIcon,
                UnresolvedIcon = KnownMonikers.PathListBox,
                UnresolvedExpandedIcon = KnownMonikers.PathListBoxItem,
                Properties = new Dictionary<string, string> { { "prop1", "val1" } }.ToImmutableDictionary()
            };

            var dependency = new Dependency(mockModel);

            Assert.Equal(mockModel.ProviderType, dependency.ProviderType);
            Assert.Equal(mockModel.Caption, dependency.Caption);
            Assert.Equal(mockModel.OriginalItemSpec, dependency.OriginalItemSpec);
            Assert.Equal(mockModel.Path, dependency.FilePath);
            Assert.Equal(mockModel.SchemaName, dependency.SchemaName);
            Assert.Equal(mockModel.SchemaItemType, dependency.SchemaItemType);
            Assert.Equal(mockModel.Resolved, dependency.Resolved);
            Assert.Equal(mockModel.Implicit, dependency.Implicit);
            Assert.Equal(mockModel.Visible, dependency.Visible);
            Assert.Single(dependency.BrowseObjectProperties);
            Assert.True(dependency.BrowseObjectProperties.ContainsKey("prop1"));
            Assert.Equal(ProjectTreeFlags.ResolvedReference + DependencyTreeFlags.Dependency, dependency.Flags);
        }

        [Fact]
        public void Dependency_WithCaption()
        {
            var dependencyModel = new TestDependencyModel { ProviderType = "providerType", Id = "someId" };

            var dependency = new Dependency(dependencyModel);

            var newDependency = dependency.WithCaption(caption: "newcaption");

            Assert.Equal("newcaption", newDependency.Caption);
        }

        [Fact]
        public void WhenCreatingADependencyFromAnotherDependency_ExistingIconSetInstanceIsReused()
        {
            var dependencyModel = new TestableDependencyModel(
                    "Caption",
                    "Path",
                    "ItemSpec",
                    iconSet: new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference));

            var dependency = new Dependency(dependencyModel);

            Assert.Same(dependencyModel.IconSet, dependency.IconSet);
        }

        private sealed class TestableDependencyModel : DependencyModel
        {
            public override string ProviderType => "someProvider";

            public override DependencyIconSet IconSet { get; }

            public TestableDependencyModel(string caption, string path, string originalItemSpec, DependencyIconSet iconSet)
                : base(caption, path, originalItemSpec, ProjectTreeFlags.Empty, isResolved: false, isImplicit: false, properties: null)
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
    }
}
