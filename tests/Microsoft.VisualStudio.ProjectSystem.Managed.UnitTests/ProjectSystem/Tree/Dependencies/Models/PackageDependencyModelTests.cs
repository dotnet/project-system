// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public class PackageDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new PackageDependencyModel(
                originalItemSpec: "myOriginalItemSpec",
                version: "myVersion",
                isResolved: true,
                isImplicit: false,
                properties: properties,
                isVisible: true);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myOriginalItemSpec (myVersion)", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(KnownMonikers.NuGetNoColor, model.Icon);
            Assert.Equal(KnownMonikers.NuGetNoColor, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.PackageDependency +
                DependencyTreeFlags.SupportsFolderBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags +
                ProjectTreeFlags.Create("$ID:myOriginalItemSpec"),
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new PackageDependencyModel(
                originalItemSpec: "myOriginalItemSpec",
                version: "myVersion",
                isResolved: false,
                isImplicit: false,
                properties: properties,
                isVisible: true);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myOriginalItemSpec (myVersion)", model.Caption);
            Assert.Equal(PackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.Visible);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(KnownMonikers.NuGetNoColor, model.Icon);
            Assert.Equal(KnownMonikers.NuGetNoColor, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.PackageDependency +
                DependencyTreeFlags.UnresolvedDependencyFlags +
                ProjectTreeFlags.Create("$ID:myOriginalItemSpec"),
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new PackageDependencyModel(
                originalItemSpec: "myOriginalItemSpec",
                version: "",
                isResolved: true,
                isImplicit: true,
                properties: properties,
                isVisible: true);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("myOriginalItemSpec", model.Caption);
            Assert.Equal(ResolvedPackageReference.SchemaName, model.SchemaName);
            Assert.Equal(PackageReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.True(model.Visible);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(KnownMonikers.NuGetNoColorPrivate, model.Icon);
            Assert.Equal(KnownMonikers.NuGetNoColorPrivate, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.NuGetNoColorWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.PackageDependency +
                DependencyTreeFlags.SupportsFolderBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags +
                ProjectTreeFlags.Create("$ID:myOriginalItemSpec") -
                DependencyTreeFlags.SupportsRemove,
                model.Flags);
        }
    }
}
