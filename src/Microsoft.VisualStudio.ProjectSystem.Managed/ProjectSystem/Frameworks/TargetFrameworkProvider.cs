// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem;

[Export(typeof(ITargetFrameworkProvider))]
internal sealed class TargetFrameworkProvider : ITargetFrameworkProvider
{
    /// <summary>
    /// Lookup for <see cref="TargetFramework"/> objects keyed by
    /// <see cref="TargetFramework.TargetFrameworkAlias"/>.
    /// </summary>
    private ImmutableDictionary<string, TargetFramework> _targetFrameworkByName = ImmutableDictionary.Create<string, TargetFramework>(StringComparer.Ordinal);

    public TargetFramework? GetTargetFramework(string? targetFrameworkMoniker)
    {
        if (Strings.IsNullOrEmpty(targetFrameworkMoniker))
        {
            return null;
        }

        // Fast path for an exact name match
        if (_targetFrameworkByName.TryGetValue(targetFrameworkMoniker, out TargetFramework? existing))
        {
            return existing;
        }

        // This is a completely new target framework. Create, cache and return it.
        var targetFramework = new TargetFramework(targetFrameworkMoniker);

        ImmutableInterlocked.TryAdd(ref _targetFrameworkByName, targetFrameworkMoniker, targetFramework);

        return targetFramework;
    }
}
