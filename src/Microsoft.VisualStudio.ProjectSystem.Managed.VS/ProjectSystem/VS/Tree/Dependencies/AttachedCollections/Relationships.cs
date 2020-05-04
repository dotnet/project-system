// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    internal static class Relationships
    {
        public static IAttachedRelationship Contains { get; } = new ContainsAttachedRelationship();

        public static IAttachedRelationship ContainedBy { get; } = new ContainedByAttachedRelationship();

        private sealed class ContainsAttachedRelationship : IAttachedRelationship
        {
            public string Name => KnownRelationships.Contains;
            public string DisplayName => KnownRelationships.Contains;
        }

        private sealed class ContainedByAttachedRelationship : IAttachedRelationship
        {
            public string Name => KnownRelationships.ContainedBy;
            public string DisplayName => KnownRelationships.ContainedBy;
        }
    }
}
