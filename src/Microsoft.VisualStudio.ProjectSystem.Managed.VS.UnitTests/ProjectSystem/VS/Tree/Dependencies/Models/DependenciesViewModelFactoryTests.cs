// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependenciesViewModelFactoryTests
    {
        [Fact]
        public void DependenciesViewModelFactoryTests_CreateTargetViewModel_NoUnresolvedDependency()
        {
            var project = UnconfiguredProjectFactory.Create();
            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tFm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                    hasUnresolvedDependency: false,
                    targetFramework: targetFramework);

            var factory = new DependenciesViewModelFactory(project);

            var result = factory.CreateTargetViewModel(targetedSnapshot);

            Assert.NotNull(result);
            Assert.Equal(targetFramework.FullName, result.Caption);
            Assert.Equal(KnownMonikers.Library, result.Icon);
            Assert.Equal(KnownMonikers.Library, result.ExpandedIcon);
            Assert.True(result.Flags.Contains(DependencyTreeFlags.TargetNodeFlags));
            Assert.True(result.Flags.Contains("$TFM:tFm1"));
        }

        [Fact]
        public void DependenciesViewModelFactoryTests_CreateTargetViewModel_HasUnresolvedDependency()
        {
            var project = UnconfiguredProjectFactory.Create();
            var targetFramework = ITargetFrameworkFactory.Implement(moniker: "tFm1");
            var targetedSnapshot = ITargetedDependenciesSnapshotFactory.Implement(
                    hasUnresolvedDependency: true,
                    targetFramework: targetFramework);

            var factory = new DependenciesViewModelFactory(project);

            var result = factory.CreateTargetViewModel(targetedSnapshot);

            Assert.NotNull(result);
            Assert.Equal(targetFramework.FullName, result.Caption);
            Assert.Equal(ManagedImageMonikers.LibraryWarning, result.Icon);
            Assert.Equal(ManagedImageMonikers.LibraryWarning, result.ExpandedIcon);
            Assert.True(result.Flags.Contains(DependencyTreeFlags.TargetNodeFlags));
            Assert.True(result.Flags.Contains("$TFM:tFm1"));
        }

        [Fact]
        public void DependenciesViewModelFactoryTests_CreateRootViewModel()
        {
            var project = UnconfiguredProjectFactory.Create();
            var dependency = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""MyProvider1"",
    ""Id"": ""ZzzDependencyRoot"",
    ""Name"":""ZzzDependencyRoot"",
    ""Caption"":""ZzzDependencyRoot""
}", icon: KnownMonikers.AboutBox);

            var subTreeProvider1 = IProjectDependenciesSubTreeProviderFactory.Implement(
                providerType: "MyProvider1", 
                createRootDependencyNode: dependency);
            var subTreeProvider2 = IProjectDependenciesSubTreeProviderFactory.Implement(
                providerType: "MyProvider2");

            var factory = new TestableDependenciesViewModelFactory(project, new[] { subTreeProvider1, subTreeProvider2 });

            var result = factory.CreateRootViewModel("MyProvider1", hasUnresolvedDependency:false);

            Assert.NotNull(result);
            Assert.Equal("ZzzDependencyRoot", result.Caption);
            Assert.Equal(KnownMonikers.AboutBox, result.Icon);
        }

        [Fact]
        public void DependenciesViewModelFactoryTests_GetDependenciesRootIcon()
        {
            var project = UnconfiguredProjectFactory.Create();
            var factory = new DependenciesViewModelFactory(project);

            Assert.Equal(ManagedImageMonikers.ReferenceGroup, factory.GetDependenciesRootIcon(hasUnresolvedDependencies: false));
            Assert.Equal(ManagedImageMonikers.ReferenceGroupWarning, factory.GetDependenciesRootIcon(hasUnresolvedDependencies: true));
        }

        private class TestableDependenciesViewModelFactory : DependenciesViewModelFactory
        {
            public TestableDependenciesViewModelFactory(UnconfiguredProject project, IEnumerable<IProjectDependenciesSubTreeProvider> providers)
                : base(project)
            {
                foreach(var provider in providers)
                {
                    SubTreeProviders.Add(new Lazy<IProjectDependenciesSubTreeProvider, IOrderPrecedenceMetadataView>(
                        () => { return provider; }, new TestPrecedenceMetadataView()));
                }
            }
        }

        private class TestPrecedenceMetadataView : IOrderPrecedenceMetadataView
        {
            public string AppliesTo => ProjectCapabilities.AlwaysApplicable;

            public int OrderPrecedence => -500;
        }
    }
}
