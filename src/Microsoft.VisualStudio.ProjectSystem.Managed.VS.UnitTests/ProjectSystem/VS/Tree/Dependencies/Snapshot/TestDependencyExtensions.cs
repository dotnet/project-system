// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal static class TestDependencyExtensions
    {
        public static void AssertEqualTo(this IDependency expected, IDependency actual)
        {
            Xunit.Assert.NotNull(actual);
            Xunit.Assert.NotNull(expected);

            Xunit.Assert.Equal(actual.ProviderType, expected.ProviderType);
            Xunit.Assert.Equal(actual.Name, expected.Name);
            Xunit.Assert.Equal(actual.Caption, expected.Caption);
            Xunit.Assert.Equal(actual.OriginalItemSpec, expected.OriginalItemSpec);
            Xunit.Assert.Equal(actual.Path, expected.Path);
            Xunit.Assert.Equal(actual.FullPath, expected.FullPath);
            Xunit.Assert.Equal(actual.SchemaName, expected.SchemaName);
            Xunit.Assert.Equal(actual.SchemaItemType, expected.SchemaItemType);
            Xunit.Assert.Equal(actual.Version, expected.Version);
            Xunit.Assert.Equal(actual.Resolved, expected.Resolved);
            Xunit.Assert.Equal(actual.TopLevel, expected.TopLevel);
            Xunit.Assert.Equal(actual.Implicit, expected.Implicit);
            Xunit.Assert.Equal(actual.Visible, expected.Visible);
            Xunit.Assert.Equal(actual.Priority, expected.Priority);
            Xunit.Assert.Equal(actual.Icon, expected.Icon, EqualityComparer<ImageMoniker>.Default);
            Xunit.Assert.Equal(actual.ExpandedIcon, expected.ExpandedIcon, EqualityComparer<ImageMoniker>.Default);
            Xunit.Assert.Equal(actual.UnresolvedIcon, expected.UnresolvedIcon, EqualityComparer<ImageMoniker>.Default);
            Xunit.Assert.Equal(actual.UnresolvedExpandedIcon, expected.UnresolvedExpandedIcon, EqualityComparer<ImageMoniker>.Default);
            Xunit.Assert.Equal(actual.Properties, expected.Properties);
            Xunit.Assert.Equal(actual.DependencyIDs, expected.DependencyIDs);
            Xunit.Assert.Equal(actual.Flags, expected.Flags);
            Xunit.Assert.Equal(actual.Id, expected.Id);
            Xunit.Assert.Equal(actual.Alias, expected.Alias);
            Xunit.Assert.Equal(actual.TargetFramework, expected.TargetFramework);
        }
    }
}
