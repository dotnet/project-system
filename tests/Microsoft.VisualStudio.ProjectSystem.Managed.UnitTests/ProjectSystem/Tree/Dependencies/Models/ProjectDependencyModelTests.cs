// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public class ProjectDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\ResolvedPath\\MyProject.dll",
                "Project\\MyProject.csproj",
                isResolved: true,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\ResolvedPath\\MyProject.dll", model.Path);
            Assert.Equal("Project\\MyProject.csproj", model.OriginalItemSpec);
            Assert.Equal("MyProject", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Application, model.Icon);
            Assert.Equal(KnownMonikers.Application, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags +
                ProjectTreeFlags.Create("$ID:MyProject"),
                model.Flags);
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\ResolvedPath\\MyProject.dll",
                "Project\\MyProject.csproj",
                isResolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\ResolvedPath\\MyProject.dll", model.Path);
            Assert.Equal("Project\\MyProject.csproj", model.OriginalItemSpec);
            Assert.Equal("MyProject", model.Caption);
            Assert.Equal(ProjectReference.SchemaName, model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.Application, model.Icon);
            Assert.Equal(KnownMonikers.Application, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsBrowse +
                DependencyTreeFlags.UnresolvedDependencyFlags +
                ProjectTreeFlags.Create("$ID:MyProject"),
                model.Flags);
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new ProjectDependencyModel(
                "c:\\ResolvedPath\\MyProject.dll",
                "Project\\MyProject.csproj",
                isResolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal(ProjectRuleHandler.ProviderTypeString, model.ProviderType);
            Assert.Equal("c:\\ResolvedPath\\MyProject.dll", model.Path);
            Assert.Equal("Project\\MyProject.csproj", model.OriginalItemSpec);
            Assert.Equal("MyProject", model.Caption);
            Assert.Equal(ResolvedProjectReference.SchemaName, model.SchemaName);
            Assert.True(model.Resolved);
            Assert.True(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(ProjectReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.ApplicationPrivate, model.Icon);
            Assert.Equal(KnownMonikers.ApplicationPrivate, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.ApplicationWarning, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.ProjectDependency +
                DependencyTreeFlags.SupportsBrowse +
                DependencyTreeFlags.ResolvedDependencyFlags -
                DependencyTreeFlags.SupportsRemove +
                ProjectTreeFlags.Create("$ID:MyProject"),
                model.Flags);
        }
    }
}
