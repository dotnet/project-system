// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public sealed class ImplicitDependenciesSnapshotFilterTests : DependenciesSnapshotFilterTestsBase
    {
        private const string ProjectItemSpec = "projectItemSpec";

        private readonly IDependency _acceptable = new TestDependency
        {
            Id = "dependency1",
            Implicit = false,
            Resolved = true,
            Flags = DependencyTreeFlags.GenericDependency,
            OriginalItemSpec = ProjectItemSpec
        };

        private protected override IDependenciesSnapshotFilter CreateFilter() => new ImplicitDependenciesSnapshotFilter();

        [Fact]
        public void BeforeAddOrUpdate_WhenImplicitAlready_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    Implicit = true
                },
                projectItemSpecs: ImmutableHashSet<string>.Empty);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenUnresolved_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    Resolved = false
                },
                projectItemSpecs: ImmutableHashSet<string>.Empty);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenNotGenericDependency_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    Flags = ProjectTreeFlags.Empty
                },
                projectItemSpecs: ImmutableHashSet<string>.Empty);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenProjectItemSpecsNull_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    Flags = ProjectTreeFlags.Empty
                },
                projectItemSpecs: null);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenSharedProject_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    Flags = DependencyTreeFlags.SharedProjectDependency
                },
                projectItemSpecs: ImmutableHashSet<string>.Empty);
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenCanApplyImplicitProjectContainsItem_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                _acceptable,
                ImmutableHashSet.Create(ProjectItemSpec));
        }

        [Fact]
        public void BeforeAddOrUpdate_WhenNeedToApplyImplicit_ShouldSetProperties()
        {
            const string providerType = "providerType";
            const string projectItemSpec = "projectItemSpec";
            var implicitIcon = KnownMonikers.Abbreviation;

            var dependency = new TestDependency
            {
                Id = "dependency1",
                ProviderType = providerType,
                Implicit = false,
                Resolved = true,
                Flags = DependencyTreeFlags.GenericDependency,
                OriginalItemSpec = projectItemSpec,
                IconSet = new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference)
            };

            var dependencyById = new Dictionary<(string ProviderType, string ModelId), IDependency>
            {
                { (dependency.ProviderType, dependency.Id), dependency }
            };

            var context = new AddDependencyContext(dependencyById);

            var filter = new ImplicitDependenciesSnapshotFilter();

            var subTreeProvider = IProjectDependenciesSubTreeProviderFactory.ImplementInternal(
                providerType: providerType,
                implicitIcon: implicitIcon);

            filter.BeforeAddOrUpdate(
                dependency,
                new Dictionary<string, IProjectDependenciesSubTreeProvider> { { providerType, subTreeProvider } },
                ImmutableHashSet<string>.Empty,
                context);

            var acceptedDependency = context.GetResult(filter);

            // Returns changed dependency
            Assert.NotNull(acceptedDependency);
            Assert.NotSame(dependency, acceptedDependency);
            DependencyAssert.Equal(
                new TestDependency
                {
                    ClonePropertiesFrom = dependency,
                    Implicit = true,
                    IconSet = new DependencyIconSet(
                        implicitIcon,
                        implicitIcon,
                        KnownMonikers.Reference,
                        KnownMonikers.Reference)
                }, acceptedDependency!);

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
