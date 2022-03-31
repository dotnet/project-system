// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
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
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SDK, model.Icon);
            Assert.Equal(KnownMonikers.SDK, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.SupportsFolderBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags,
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
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SDK, model.Icon);
            Assert.Equal(KnownMonikers.SDK, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.UnresolvedDependencyFlags,
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
            Assert.Equal(SdkReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.SDKPrivate, model.Icon);
            Assert.Equal(KnownMonikers.SDKPrivate, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.SDKWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.SdkDependency +
                DependencyTreeFlags.SupportsFolderBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
