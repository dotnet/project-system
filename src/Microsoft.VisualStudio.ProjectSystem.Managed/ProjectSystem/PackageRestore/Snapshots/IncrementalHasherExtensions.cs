// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

internal static class IncrementalHasherExtensions
{
    public static void AppendProperty(this IncrementalHasher hasher, string name, string value)
    {
        hasher.Append(name);
        hasher.Append("|");
        hasher.Append(value);
    }

    public static void AppendArray<T>(this IncrementalHasher hasher, ImmutableArray<T> items) where T : class, IRestoreState<T>
    {
        foreach (T item in items)
        {
            item.AddToHash(hasher);
        }
    }
}
