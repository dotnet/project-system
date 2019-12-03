// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class SdkDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("Version", "2.0.0");

            var model = new SdkDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(SdkRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal(ResolvedSdkReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Sdk, model.Icon);
            Assert.Equal(ManagedImageMonikers.Sdk, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("Version", "2.0.0");

            var model = new SdkDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(SdkRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal(SdkReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.Sdk, model.Icon);
            Assert.Equal(ManagedImageMonikers.Sdk, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("Version", "2.0.0");

            var model = new SdkDependencyModel(
                "c:\\myPath.dll",
                "myOriginalItemSpec",
                isResolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(SdkRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath.dll", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath.dll (2.0.0)", model.Caption);
            Assert.Equal(ResolvedSdkReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.SdkNodePriority, model.Priority);
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.SdkPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.SdkPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.SdkWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.SupportsHierarchy +
                DependencyTreeFlags.GenericResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
