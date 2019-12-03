// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageUnknownDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");
            var dependencyIDs = new[] { "id1", "id2" };

            var model = new PackageUnknownDependencyModel(
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
            Assert.Equal(GraphNodePriority.UnresolvedReference, model.Priority);
            Assert.Equal(KnownMonikers.QuestionMark, model.Icon);
            Assert.Equal(KnownMonikers.QuestionMark, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.QuestionMark, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.QuestionMark, model.UnresolvedExpandedIcon);
            AssertEx.CollectionLength(model.DependencyIDs, 2);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.GenericResolvedDependencyFlags,
                model.Flags);
        }
    }
}
