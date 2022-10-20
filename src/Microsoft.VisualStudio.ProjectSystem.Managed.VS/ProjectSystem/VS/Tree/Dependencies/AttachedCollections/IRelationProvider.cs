// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides the set of exported <see cref="IRelation"/> instances that apply to given types,
    /// both for contains and contained-by relationships.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IRelationProvider
    {
        /// <summary>
        /// Gets the set of <see cref="IRelation"/> instances that can produce child (contained) items for parent items of type <paramref name="parentType"/>.
        /// If no relations are found, <see cref="ImmutableArray{T}.Empty"/> is returned.
        /// </summary>
        ImmutableArray<IRelation> GetContainsRelationsFor(Type parentType);

        /// <summary>
        /// Gets the set of <see cref="IRelation"/> instances that can produce parent (contained by) items for child items of type <paramref name="childType"/>.
        /// If no relations are found, <see cref="ImmutableArray{T}.Empty"/> is returned.
        /// </summary>
        ImmutableArray<IRelation> GetContainedByRelationsFor(Type childType);
    }
}
