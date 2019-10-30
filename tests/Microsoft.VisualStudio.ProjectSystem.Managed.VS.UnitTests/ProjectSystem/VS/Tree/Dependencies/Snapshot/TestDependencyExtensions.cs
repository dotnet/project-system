// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class DependencyAssert
    {
        public static void Equal(IDependency expected, IDependency actual)
        {
            Xunit.Assert.NotNull(actual);
            Xunit.Assert.NotNull(expected);

            Xunit.Assert.Equal(expected.ProviderType, actual.ProviderType);
            Xunit.Assert.Equal(expected.Name, actual.Name);
            Xunit.Assert.Equal(expected.Caption, actual.Caption);
            Xunit.Assert.Equal(expected.OriginalItemSpec, actual.OriginalItemSpec);
            Xunit.Assert.Equal(expected.Path, actual.Path);
            Xunit.Assert.Equal(expected.FullPath, actual.FullPath);
            Xunit.Assert.Equal(expected.SchemaName, actual.SchemaName);
            Xunit.Assert.Equal(expected.SchemaItemType, actual.SchemaItemType);
            Xunit.Assert.Equal(expected.Resolved, actual.Resolved);
            Xunit.Assert.Equal(expected.TopLevel, actual.TopLevel);
            Xunit.Assert.Equal(expected.Implicit, actual.Implicit);
            Xunit.Assert.Equal(expected.Visible, actual.Visible);
            Xunit.Assert.Equal(expected.Priority, actual.Priority);
            Xunit.Assert.Equal(expected.IconSet, actual.IconSet);
            Xunit.Assert.Equal(expected.BrowseObjectProperties, actual.BrowseObjectProperties);
            Xunit.Assert.Equal(expected.DependencyIDs, actual.DependencyIDs);
            Xunit.Assert.Equal(expected.Flags, actual.Flags);
            Xunit.Assert.Equal(expected.Id, actual.Id);
            Xunit.Assert.Equal(expected.TargetFramework, actual.TargetFramework);
        }
    }
}
