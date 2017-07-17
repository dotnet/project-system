// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependencyModelTests
    {
        [Fact]
        public void Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("providerType", () =>
            {
                new DependencyModel(null, null, "", ProjectTreeFlags.Empty, false, false, null);
            });

            Assert.Throws<ArgumentNullException>("path", () =>
            {
                new DependencyModel("sometype", null, "", ProjectTreeFlags.Empty, false, false, null);
            });            
        }

        [Fact]
        public void Constructor_WhenOptionalValuesNotProvided_ShouldSetDefaults()
        {
            var model = new DependencyModel(
                providerType: "somProvider", 
                path: "somePath",
                originalItemSpec: null, 
                flags:ProjectTreeFlags.Empty, 
                resolved:false, 
                isImplicit:false, 
                properties:null);

            Assert.Equal("somePath", model.OriginalItemSpec);
            Assert.Equal(ImmutableDictionary<string, string>.Empty, model.Properties);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_UnresolvedAndNotImplicit()
        {
            var model = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: false,
                isImplicit: false,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version:"version1\\");

            Assert.Equal("SomeItemSpec\\version1", model.Id);
            Assert.Equal("somProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.True(model.Flags.Contains(ProjectTreeFlags.HiddenProjectItem));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.GenericUnresolvedDependencyFlags));
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_ResolvedAndNotImplicit()
        {
            var model = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: false,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version: "version1\\");

            Assert.Equal("SomeItemSpec\\version1", model.Id);
            Assert.Equal("somProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.True(model.Flags.Contains(ProjectTreeFlags.HiddenProjectItem));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.GenericResolvedDependencyFlags));
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void Constructor_WhenValidParametersProvided_ResolvedAndImplicit()
        {
            var model = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version: "version1\\");

            Assert.Equal("SomeItemSpec\\version1", model.Id);
            Assert.Equal("somProvider", model.ProviderType);
            Assert.Equal("somePath", model.Path);
            Assert.Equal("SomeItemSpec", model.OriginalItemSpec);
            Assert.True(model.Flags.Contains(ProjectTreeFlags.HiddenProjectItem));
            Assert.True(model.Flags.Contains(DependencyTreeFlags.GenericResolvedDependencyFlags.Except(DependencyTreeFlags.SupportsRemove)));
            Assert.False(model.Flags.Contains(DependencyTreeFlags.SupportsRemove));
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void EqualsAndGetHashCode()
        {
            var model1 = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec1",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version: "versio1\\");

            var model2 = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec1",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version: "versio1\\");

            var model3 = new TestableDependencyModel(
                providerType: "somProvider",
                path: "somePath",
                originalItemSpec: "SomeItemSpec2",
                flags: ProjectTreeFlags.HiddenProjectItem,
                resolved: true,
                isImplicit: true,
                properties: ImmutableDictionary<string, string>.Empty.Add("someProp1", "someVal1"),
                version: "versio1\\");

            Assert.Equal(model1, model2);
            Assert.NotEqual(model1, model3);

            Assert.Equal(model1.GetHashCode(), model2.GetHashCode());
            Assert.NotEqual(model1.GetHashCode(), model3.GetHashCode());
        }

        [Fact]
        public void Visible_True()
        {
            var dependencyModel = new DependencyModel(
                providerType: "someProvider",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: ImmutableDictionary<string, string>.Empty.Add("Visible", "true"));

            Assert.True(dependencyModel.Visible);
        }

        [Fact]
        public void Visible_False()
        {
            var dependencyModel = new DependencyModel(
                providerType: "someProvider",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: ImmutableDictionary<string, string>.Empty.Add("Visible", "false"));

            Assert.False(dependencyModel.Visible);
        }

        [Fact]
        public void Visible_TrueWhenNotSpecified()
        {
            var dependencyModel = new DependencyModel(
                providerType: "someProvider",
                path: "somePath",
                originalItemSpec: "someItemSpec",
                flags: ProjectTreeFlags.Empty,
                resolved: true,
                isImplicit: false,
                properties: null);

            Assert.True(dependencyModel.Visible);
        }

        private class TestableDependencyModel : DependencyModel
        {
            public TestableDependencyModel(
                string providerType,
                string path,
                string originalItemSpec,
                ProjectTreeFlags flags,
                bool resolved,
                bool isImplicit,
                IImmutableDictionary<string, string> properties,
                string version)
                : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
            {
                Version = version;
            }
        }
    }
}
