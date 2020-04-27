// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Enumeration of package assembly group types.
    /// </summary>
    /// <remarks>
    /// Use by <see cref="PackageAssemblyGroupItem"/>, <see cref="PackageAssemblyItem"/> and their <see cref="IRelation"/> types.
    /// </remarks>
    internal enum PackageAssemblyGroupType
    {
        CompileTime,
        Framework
    }
}
