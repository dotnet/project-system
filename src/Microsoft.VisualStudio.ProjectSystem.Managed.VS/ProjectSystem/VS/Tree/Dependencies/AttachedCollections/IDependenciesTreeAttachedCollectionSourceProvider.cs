// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Implementations can attach descendants to top-level items in the dependencies tree.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IDependenciesTreeAttachedCollectionSourceProvider
    {
        // TODO this interface will become the public extensibility point
        // TODO add members to support search

        IAttachedCollectionSource? TryCreateSource(IVsHierarchyItem hierarchyItem);

        ref readonly HierarchyItemFlagsDetector FlagsDetector { get; }
    }
}
