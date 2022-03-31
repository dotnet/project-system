// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public sealed class DependencyModelTests
    {
        private class TestableDependencyModel : DependencyModel
        {
            public override string ProviderType => "someProvider";

            public override DependencyIconSet IconSet => new(
                KnownMonikers.Accordian,
                KnownMonikers.Bug,
                KnownMonikers.CrashDumpFile,
                KnownMonikers.DataCenter,
                KnownMonikers.Edit,
                KnownMonikers.F1Help);

            public TestableDependencyModel(
                string caption,
                string? path,
                string originalItemSpec,
                ProjectTreeFlags flags,
                bool resolved,
                bool isImplicit,
                IImmutableDictionary<string, string>? properties)
                : base(caption, path, originalItemSpec, flags, resolved, isImplicit, properties)
            {
            }
        }

        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("caption", () =>
            {
                new TestableDependencyModel(null!, null, "itemSpec", ProjectTreeFlags.Empty, false, false, null);
            });
            Assert.Throws<ArgumentNullException>("originalItemSpec", () =>
            {
                new TestableDependencyModel("Caption", null, null!, ProjectTreeFlags.Empty, false, false, null);
            });

            // Empty caption is also disallowed
            Assert.Throws<ArgumentException>("caption", () =>
            {
                new TestableDependencyModel("", null, "itemSpec", ProjectTreeFlags.Empty, false, false, null);
            });
        }

        [Fact]
        public void Constructor_NullProperties_SetsEmptyCollection()
        {
            var model = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "originalItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: false,
                isImplicit: false,
                properties: null);

            Assert.Equal(ImmutableStringDictionary<string>.EmptyOrdinal, model.Properties);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_UnresolvedAndNotImplicit()
        {
            var model = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: false,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

            Assert.Equal("Caption", model.Caption);
            Assert.Equal("SomeItemSpec", model.Id);
            Assert.Equal("someProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.Equal(ProjectTreeFlags.HiddenProjectItem, model.Flags);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Single(model.Properties);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_ResolvedAndNotImplicit()
        {
            var model = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

            Assert.Equal("Caption", model.Caption);
            Assert.Equal("SomeItemSpec", model.Id);
            Assert.Equal("someProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.Equal(ProjectTreeFlags.HiddenProjectItem, model.Flags);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Single(model.Properties);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_ResolvedAndImplicit()
        {
            var model = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

            Assert.Equal("Caption", model.Caption);
            Assert.Equal("SomeItemSpec", model.Id);
            Assert.Equal("someProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.Equal(ProjectTreeFlags.HiddenProjectItem, model.Flags);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Single(model.Properties);
        }

        [Fact]
        public void Visible_True()
        {
            var dependencyModel = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("Visible", "true"));

            Assert.True(dependencyModel.Visible);
        }

        [Fact]
        public void Visible_False()
        {
            var dependencyModel = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("Visible", "false"));

            Assert.False(dependencyModel.Visible);
        }

        [Fact]
        public void Visible_TrueWhenNotSpecified()
        {
            var dependencyModel = new TestableDependencyModel(
                caption: "Caption",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: null);

            Assert.True(dependencyModel.Visible);
        }
    }
}
