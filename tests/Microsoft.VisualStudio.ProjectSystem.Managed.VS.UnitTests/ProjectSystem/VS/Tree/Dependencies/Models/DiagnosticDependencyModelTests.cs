// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class DiagnosticDependencyModelTests
    {
        [Fact]
        public void Error()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new DiagnosticDependencyModel(
                "myOriginalItemSpec",
                DiagnosticMessageSeverity.Error,
                "nu1002",
                "myMessage",
                isVisible: true,
                properties: properties);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
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
            Assert.Equal(GraphNodePriority.DiagnosticsError, model.Priority);
            Assert.Null(model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.Icon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.ErrorSmall, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.Diagnostic +
                DependencyTreeFlags.ErrorDiagnostic +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }

        [Fact]
        public void Warning()
        {
            var properties = ImmutableStringDictionary<string>.EmptyOrdinal.Add("myProp", "myVal");

            var model = new DiagnosticDependencyModel(
                 "myOriginalItemSpec",
                 DiagnosticMessageSeverity.Warning,
                 "nu1002",
                 "myMessage",
                 isVisible: true,
                 properties: properties);

            Assert.Equal(PackageRuleHandler.ProviderTypeString, model.ProviderType);
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
            Assert.Equal(GraphNodePriority.DiagnosticsWarning, model.Priority);
            Assert.Null(model.SchemaItemType);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.Icon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.ExpandedIcon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.UnresolvedIcon);
            Assert.Equal(ManagedImageMonikers.WarningSmall, model.UnresolvedExpandedIcon);
            Assert.Equal(
                DependencyTreeFlags.NuGetDependency +
                DependencyTreeFlags.Diagnostic +
                DependencyTreeFlags.WarningDiagnostic +
                DependencyTreeFlags.GenericUnresolvedDependencyFlags,
                model.Flags);
        }
    }
}
