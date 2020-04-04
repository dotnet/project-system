// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public class DependenciesViewModelFactoryTests
    {
        [Fact]
        public void CreateTargetViewModel_NoUnresolvedDependency()
        {
            var project = UnconfiguredProjectFactory.Create();
            var targetFramework = new TargetFramework(moniker: "tFm1");

            var factory = new DependenciesViewModelFactory(project);

            var result = factory.CreateTargetViewModel(targetFramework, hasVisibleUnresolvedDependency: false);

            Assert.NotNull(result);
            Assert.Equal(targetFramework.FullName, result.Caption);
            Assert.Equal(KnownMonikers.Library, result.Icon);
            Assert.Equal(KnownMonikers.Library, result.ExpandedIcon);
            Assert.True(result.Flags.Contains(DependencyTreeFlags.TargetNode));
            Assert.True(result.Flags.Contains("$TFM:tFm1"));
        }

        [Fact]
        public void CreateTargetViewModel_HasUnresolvedDependency()
        {
            var project = UnconfiguredProjectFactory.Create();
            var targetFramework = new TargetFramework(moniker: "tFm1");

            var factory = new DependenciesViewModelFactory(project);

            var result = factory.CreateTargetViewModel(targetFramework, hasVisibleUnresolvedDependency: true);

            Assert.NotNull(result);
            Assert.Equal(targetFramework.FullName, result.Caption);
            Assert.Equal(ManagedImageMonikers.LibraryWarning, result.Icon);
            Assert.Equal(ManagedImageMonikers.LibraryWarning, result.ExpandedIcon);
            Assert.True(result.Flags.Contains(DependencyTreeFlags.TargetNode));
            Assert.True(result.Flags.Contains("$TFM:tFm1"));
        }

        [Fact]
        public void CreateGroupNodeViewModel()
        {
            var project = UnconfiguredProjectFactory.Create();

            var dependencyModel = new TestDependencyModel
            {
                ProviderType = "MyProvider1",
                Id = "ZzzDependencyRoot",
                Name = "ZzzDependencyRoot",
                Caption = "ZzzDependencyRoot",
                Icon = KnownMonikers.AboutBox
            };

            var subTreeProvider1 = IProjectDependenciesSubTreeProviderFactory.Implement(
                providerType: "MyProvider1",
                createRootDependencyNode: dependencyModel);
            var subTreeProvider2 = IProjectDependenciesSubTreeProviderFactory.Implement(
                providerType: "MyProvider2");

            var factory = new TestableDependenciesViewModelFactory(project, new[] { subTreeProvider1, subTreeProvider2 });

            var result = factory.CreateGroupNodeViewModel("MyProvider1", hasVisibleUnresolvedDependency: false);

            Assert.NotNull(result);
            Assert.Equal("ZzzDependencyRoot", result!.Caption);
            Assert.Equal(KnownMonikers.AboutBox, result.Icon);
        }

        [Fact]
        public void CreateGroupNodeViewModel_ReturnsNullForUnknownProviderType()
        {
            var project = UnconfiguredProjectFactory.Create();

            var subTreeProvider1 = IProjectDependenciesSubTreeProviderFactory.Implement(providerType: "MyProvider1");

            var factory = new TestableDependenciesViewModelFactory(project, new[] { subTreeProvider1 });

            var result = factory.CreateGroupNodeViewModel("UnknownProviderType", hasVisibleUnresolvedDependency: false);

            Assert.Null(result);
        }

        [Fact]
        public void GetDependenciesRootIcon()
        {
            var project = UnconfiguredProjectFactory.Create();
            var factory = new DependenciesViewModelFactory(project);

            Assert.Equal(ManagedImageMonikers.ReferenceGroup, factory.GetDependenciesRootIcon(hasVisibleUnresolvedDependency: false));
            Assert.Equal(ManagedImageMonikers.ReferenceGroupWarning, factory.GetDependenciesRootIcon(hasVisibleUnresolvedDependency: true));
        }

        private class TestableDependenciesViewModelFactory : DependenciesViewModelFactory
        {
            public TestableDependenciesViewModelFactory(UnconfiguredProject project, IEnumerable<IProjectDependenciesSubTreeProvider> providers)
                : base(project)
            {
                foreach (var provider in providers)
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

            public bool SuppressLowerPriority => false;
        }
    }
}
