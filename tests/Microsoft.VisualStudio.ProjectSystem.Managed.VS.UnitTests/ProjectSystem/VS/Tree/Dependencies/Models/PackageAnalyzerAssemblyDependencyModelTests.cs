// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageAnalyzerAssemblyDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageAnalyzerAssemblyDependencyModel(
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                isResolved: true,
                properties: properties,
                dependenciesIDs: dependencyIDs);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Null(model.SchemaName);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.PackageAssembly, model.Priority);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.PackageAnalyzerDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageAnalyzerAssemblyDependencyModel(
                path: "c:\\myPath",
                originalItemSpec: "myOriginalItemSpec",
                name: "myPath",
                isResolved: false,
                properties: properties,
                dependenciesIDs: dependencyIDs);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myPath", model.Name);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Null(model.SchemaName);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(GraphNodePriority.UnresolvedReference, model.Priority);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.PackageAnalyzerDependency +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }
    }
}
