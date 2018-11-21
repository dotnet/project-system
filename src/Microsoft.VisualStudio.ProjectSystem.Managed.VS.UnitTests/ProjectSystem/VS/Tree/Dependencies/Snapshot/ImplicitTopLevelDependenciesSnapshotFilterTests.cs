// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class ImplicitTopLevelDependenciesSnapshotFilterTests : DependenciesSnapshotFilterTestsBase
    {
        private const string ProjectItemSpec = "projectItemSpec";

        private readonly IDependency _acceptable = new TestDependency
        {
            Id = "dependency1",
            TopLevel = true,
            Implicit = false,
            Resolved = true,
            Flags = DependencyTreeFlags.GenericDependencyFlags,
            OriginalItemSpec = ProjectItemSpec
        };

        private protected override IDependenciesSnapshotFilter CreateFilter() => new ImplicitTopLevelDependenciesSnapshotFilter();

        [Fact]
        public void BeforeAddOrUpdate_WhenNotTopLevel_ShouldDoNothing()
        {
            VerifyUnchangedOnAdd(
                new TestDependency
                {
                    ClonePropertiesFrom = _acceptable,
                    TopLevel = false
                },
                projectItemSpecs: ImmutableHashSet<string>.Empty);
        }

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
                TopLevel = true,
                Implicit = false,
                Resolved = true,
                Flags = DependencyTreeFlags.GenericDependencyFlags,
                OriginalItemSpec = projectItemSpec,
                IconSet = new DependencyIconSet(KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference, KnownMonikers.Reference)
            };

            var worldBuilder = new IDependency[] { dependency }.ToImmutableDictionary(d => d.Id).ToBuilder();

            var context = new AddDependencyContext(worldBuilder);

            var filter = new ImplicitTopLevelDependenciesSnapshotFilter();

            var subTreeProvider = IProjectDependenciesSubTreeProviderFactory.ImplementInternal(
                providerType: providerType,
                implicitIcon: implicitIcon);

            filter.BeforeAddOrUpdate(
                null,
                null,
                dependency,
                new Dictionary<string, IProjectDependenciesSubTreeProvider> { { providerType, subTreeProvider } },
                ImmutableHashSet<string>.Empty,
                context);
            
            var acceptedDependency = context.GetResult(filter);

            // Returns changed dependency
            Assert.NotNull(acceptedDependency);
            Assert.NotSame(dependency, acceptedDependency);

            acceptedDependency.AssertEqualTo(
                new TestDependency
                {
                    ClonePropertiesFrom = dependency,
                    Implicit = true,
                    IconSet = new DependencyIconSet(
                        implicitIcon,
                        implicitIcon,
                        KnownMonikers.Reference,
                        KnownMonikers.Reference)
                });

            // No other changes made
            Assert.False(context.Changed);
        }
    }
}
