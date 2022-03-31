// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static partial class ProjectRuleSnapshotExtensions
    {
        private class EmptyProjectRuleSnapshot : IProjectRuleSnapshot
        {
            public static readonly EmptyProjectRuleSnapshot Instance = new();

            public string RuleName
            {
                get { throw new NotSupportedException(); }
            }

            public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items
            {
                get { return ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty; }
            }

            public IImmutableDictionary<string, string> Properties
            {
                get { return ImmutableDictionary<string, string>.Empty; }
            }
        }
    }
}
