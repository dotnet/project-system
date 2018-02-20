// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Trait("UnitTest", "ProjectSystem")]
    public class DiagnosticDependencyModelTests
    {
        [Fact]
        public void Error()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new DiagnosticDependencyModel(
                "myProvider",
                "myOriginalItemSpec",
                DiagnosticMessageSeverity.Error,
                "nu1002",
                "myMessage",
                flags: flag,
                isVisible: true,
                properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("myOriginalItemSpec", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("NU1002 myMessage", model.Caption);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.Null(model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.DiagnosticsErrorNodePriority, model.Priority);
            Assert.Null(model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.Icon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.DiagnosticErrorNodeFlags));
            Assert.True(model.Flags.Contains(flag));
        }

        [Fact]
        public void Warning()
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add("myProp", "myVal");

            var flag = ProjectTreeFlags.Create("MyCustomFlag");
            var model = new DiagnosticDependencyModel(
                 "myProvider",
                 "myOriginalItemSpec",
                 DiagnosticMessageSeverity.Warning,
                 "nu1002",
                 "myMessage",
                 flags: flag,
                 isVisible: true,
                 properties: properties);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("myOriginalItemSpec", model.Path);
            Assert.Equal("myOriginalItemSpec", model.OriginalItemSpec);
            Assert.Equal("myOriginalItemSpec", model.Id);
            Assert.Equal("NU1002 myMessage", model.Caption);
            Assert.False(model.TopLevel);
            Assert.True(model.Visible);
            Assert.Null(model.SchemaName);
            Assert.False(model.Resolved);
            Assert.False(model.Implicit);
            Assert.Equal(properties, model.Properties);
            Assert.Equal(Dependency.DiagnosticsWarningNodePriority, model.Priority);
            Assert.Null(model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.Icon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.UnresolvedExpandedIcon);
            Assert.True(model.Flags.Contains(DependencyTreeFlags.DiagnosticWarningNodeFlags));
            Assert.True(model.Flags.Contains(flag));
        }
    }
}
