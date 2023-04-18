// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal static class DependencyAssert
    {
        public static void Equal(IDependency expected, IDependency actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);

            Assert.Equal(expected.ProviderType, actual.ProviderType);
            Assert.Equal(expected.Caption, actual.Caption);
            Assert.Equal(expected.OriginalItemSpec, actual.OriginalItemSpec);
            Assert.Equal(expected.FilePath, actual.FilePath);
            Assert.Equal(expected.SchemaName, actual.SchemaName);
            Assert.Equal(expected.SchemaItemType, actual.SchemaItemType);
            Assert.Equal(expected.Resolved, actual.Resolved);
            Assert.Equal(expected.Implicit, actual.Implicit);
            Assert.Equal(expected.Visible, actual.Visible);
            Assert.Equal(expected.IconSet, actual.IconSet);
            Assert.Equal(expected.BrowseObjectProperties, actual.BrowseObjectProperties);
            Assert.Equal(expected.Flags, actual.Flags);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.DiagnosticLevel, actual.DiagnosticLevel);
        }
    }
}
