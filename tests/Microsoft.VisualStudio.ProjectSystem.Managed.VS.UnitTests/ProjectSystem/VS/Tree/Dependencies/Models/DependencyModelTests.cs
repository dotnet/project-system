// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class DependencyModelTests
    {
        private class TestableDependencyModel : DependencyModel
        {
            public override string ProviderType => "someProvider";

            public override DependencyIconSet IconSet => new DependencyIconSet(Icon, ExpandedIcon, UnresolvedIcon, UnresolvedExpandedIcon);

            public TestableDependencyModel(
                string path,
                string originalItemSpec,
                ProjectTreeFlags flags,
                bool resolved,
                bool isImplicit,
                IImmutableDictionary<string, string>? properties)
                : base(path, originalItemSpec, flags, resolved, isImplicit, properties)
            {
            }
        }

        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("path", () =>
            {
                new TestableDependencyModel(null!, "", ProjectTreeFlags.Empty, false, false, null);
            });
        }

        [Fact]
        public void Constructor_NullProperties_SetsEmptyCollection()
        {
            var model = new TestableDependencyModel(
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
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: false,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

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
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: false,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

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
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableStringDictionary<string>.EmptyOrdinal.Add("someProp1", "someVal1"));

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
