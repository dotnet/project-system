// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public class AnalyzerDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new AnalyzerDependencyModel(
                "c:\\myPath",
                "myOriginalItemSpec",
                resolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(AnalyzerRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedAnalyzerReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.AnalyzerDependency +
                DependencyTreeFlags.SupportsBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags +
                ProjectTreeFlags.FileSystemEntity,
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new AnalyzerDependencyModel(
                "c:\\myPath",
                "myOriginalItemSpec",
                resolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(AnalyzerRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath", model.Caption);
            Assert.Equal(AnalyzerReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.AnalyzerDependency +
                DependencyTreeFlags.SupportsBrowse +
                DependencyTreeFlags.UnresolvedDependencyFlags +
                ProjectTreeFlags.FileSystemEntity,
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new AnalyzerDependencyModel(
                "c:\\myPath",
                "myOriginalItemSpec",
                resolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(AnalyzerRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedAnalyzerReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.CodeInformationPrivate, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformationPrivate, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.AnalyzerDependency +
                DependencyTreeFlags.SupportsBrowse + 
                DependencyTreeFlags.ResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove +
                ProjectTreeFlags.FileSystemEntity,
                model.Flags);
        }
    }
}
