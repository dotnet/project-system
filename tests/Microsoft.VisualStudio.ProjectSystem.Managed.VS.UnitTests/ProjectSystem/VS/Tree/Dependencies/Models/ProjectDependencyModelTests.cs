// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class ProjectDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.Project, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Application, model.Icon);
            Assert.Equal(KnownMonikers.Application, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ProjectReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.Project, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Application, model.Icon);
            Assert.Equal(KnownMonikers.Application, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.SupportsHierarchy));
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.Project, model.Priority);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.ApplicationPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.ApplicationPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
