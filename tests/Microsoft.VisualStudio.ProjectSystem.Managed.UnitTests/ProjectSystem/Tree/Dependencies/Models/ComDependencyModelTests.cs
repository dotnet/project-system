// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public class ComDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ComDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ComRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedCOMReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ComReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Component, model.Icon);
            Assert.Equal(ManagedImageMonikers.Component, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ComDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ComDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ComRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath.dll", model.Caption);
            Assert.Equal(ComReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ComReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Component, model.Icon);
            Assert.Equal(ManagedImageMonikers.Component, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ComDependency +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ComDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(ComRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedCOMReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ComReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.ComponentPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.ComponentPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ComponentWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ComDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
