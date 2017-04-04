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
        public void DependencyModel_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
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
        public void DependencyModel_Constructor_WhenOptionalValuesNotProvided_ShouldSetDefaults()
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
        public void DependencyModel_Constructor_WhenValidParametersProvided_UnresolvedAndNotImplicit()
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
            Assert.Equal(false, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void DependencyModel_Constructor_WhenValidParametersProvided_ResolvedAndNotImplicit()
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
            Assert.Equal(true, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void DependencyModel_Constructor_WhenValidParametersProvided_ResolvedAndImplicit()
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
            Assert.Equal(true, model.Resolved);
            Assert.Equal(true, model.Implicit);
            Assert.Equal(1, model.Properties.Count);
        }

        [Fact]
        public void DependencyModel_EqualsAndGetHashCode()
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
            Assert.Equal("someitemspec1\\versio1".GetHashCode(), model1.GetHashCode());
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
