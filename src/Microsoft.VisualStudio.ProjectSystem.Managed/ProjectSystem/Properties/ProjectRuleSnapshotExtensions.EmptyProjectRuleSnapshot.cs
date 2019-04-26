// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static partial class ProjectRuleSnapshotExtensions
    {
        private class EmptyProjectRuleSnapshot : IProjectRuleSnapshot
        {
            public static readonly EmptyProjectRuleSnapshot Instance = new EmptyProjectRuleSnapshot();

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
