// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
/// A data object at some level within the restore state snapshot, <see cref="ProjectRestoreInfo"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IRestoreState<T> where T : class
{
    /// <summary>
    /// Adds state from this object to <paramref name="hasher"/>.
    /// </summary>
    /// <param name="hasher"></param>
    void AddToHash(IncrementalHasher hasher);

    /// <summary>
    /// Compares all state between this instance and <paramref name="after"/>, and logs details of any changes.
    /// </summary>
    void DescribeChanges(RestoreStateComparisonBuilder builder, T after);
}
