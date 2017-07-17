// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class AnalyzerDependencyModelTests
    {
        [Fact]
        public void Resolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AnalyzerDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags:flag,
                resolved:true,
                isImplicit:false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedAnalyzerReference.SchemaName, model.SchemaName);
            Assert.Equal(true, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.AnalyzerNodePriority, model.Priority);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Unresolved()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AnalyzerDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: false,
                isImplicit: false,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("c:\\myPath", model.Caption);
            Assert.Equal(AnalyzerReference.SchemaName, model.SchemaName);
            Assert.Equal(false, model.Resolved);
            Assert.Equal(false, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.AnalyzerNodePriority, model.Priority);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(KnownMonikers.CodeInformation, model.Icon);
            Assert.Equal(KnownMonikers.CodeInformation, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Implicit()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new AnalyzerDependencyModel(
                "myProvider",
                "c:\\myPath",
                "myOriginalItemSpec",
                flags: flag,
                resolved: true,
                isImplicit: true,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("c:\\myPath", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myPath", model.Caption);
            Assert.Equal(ResolvedAnalyzerReference.SchemaName, model.SchemaName);
            Assert.Equal(true, model.Resolved);
            Assert.Equal(true, model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.AnalyzerNodePriority, model.Priority);
            Assert.Equal(AnalyzerReference.PrimaryDataSourceItemType, model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.CodeInformationPrivate, model.Icon);
            Assert.Equal(ManagedImageMonikers.CodeInformationPrivate, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.CodeInformationWarning, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
