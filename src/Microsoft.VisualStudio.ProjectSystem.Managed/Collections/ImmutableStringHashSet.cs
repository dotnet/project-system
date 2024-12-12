﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio;

namespace System.Collections.Immutable;

internal static class ImmutableStringHashSet
{
    public static readonly ImmutableHashSet<string> EmptyOrdinal = ImmutableHashSet<string>.Empty;

    public static readonly ImmutableHashSet<string> EmptyOrdinalIgnoreCase = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);

    // For semantic string comparers.

    public static readonly ImmutableHashSet<string> EmptyRuleNames = ImmutableHashSet.Create<string>(StringComparers.RuleNames);

    public static readonly ImmutableHashSet<string> EmptyVisualStudioSetupComponentIds = ImmutableHashSet.Create<string>(StringComparers.VisualStudioSetupComponentIds);
}
