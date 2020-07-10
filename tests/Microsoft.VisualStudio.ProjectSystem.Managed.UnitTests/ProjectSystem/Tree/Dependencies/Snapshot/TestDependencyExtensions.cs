// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal static class DependencyAssert
    {
        public static void Equal(IDependency expected, IDependency actual)
        {
            Xunit.Assert.NotNull(actual);
            Xunit.Assert.NotNull(expected);

            Xunit.Assert.Equal(expected.ProviderType, actual.ProviderType);
            Xunit.Assert.Equal(expected.Caption, actual.Caption);
            Xunit.Assert.Equal(expected.OriginalItemSpec, actual.OriginalItemSpec);
            Xunit.Assert.Equal(expected.FilePath, actual.FilePath);
            Xunit.Assert.Equal(expected.SchemaName, actual.SchemaName);
            Xunit.Assert.Equal(expected.SchemaItemType, actual.SchemaItemType);
            Xunit.Assert.Equal(expected.Resolved, actual.Resolved);
            Xunit.Assert.Equal(expected.Implicit, actual.Implicit);
            Xunit.Assert.Equal(expected.Visible, actual.Visible);
            Xunit.Assert.Equal(expected.IconSet, actual.IconSet);
            Xunit.Assert.Equal(expected.BrowseObjectProperties, actual.BrowseObjectProperties);
            Xunit.Assert.Equal(expected.Flags, actual.Flags);
            Xunit.Assert.Equal(expected.Id, actual.Id);
            Xunit.Assert.Equal(expected.DiagnosticLevel, actual.DiagnosticLevel);
        }
    }
}
