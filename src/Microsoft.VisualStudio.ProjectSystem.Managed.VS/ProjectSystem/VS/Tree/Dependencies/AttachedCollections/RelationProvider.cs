// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    [Export(typeof(IRelationProvider))]
    internal sealed class RelationProvider : IRelationProvider
    {
        private readonly IEnumerable<IRelation> _allRelations;
        private ImmutableDictionary<Type, ImmutableArray<IRelation>> _containsRelationsByTypes = ImmutableDictionary<Type, ImmutableArray<IRelation>>.Empty;
        private ImmutableDictionary<Type, ImmutableArray<IRelation>> _containedByRelationsByTypes = ImmutableDictionary<Type, ImmutableArray<IRelation>>.Empty;

        [ImportingConstructor]
        public RelationProvider([ImportMany] IEnumerable<IRelation> allRelations)
        {
            _allRelations = allRelations;
        }

        public ImmutableArray<IRelation> GetContainsRelationsFor(Type parentType)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _containsRelationsByTypes,
                parentType,
                (t, all) => all.Where(relation => relation.SupportsContainsFor(t)).ToImmutableArray(),
                _allRelations);
        }

        public ImmutableArray<IRelation> GetContainedByRelationsFor(Type childType)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _containedByRelationsByTypes,
                childType,
                (t, all) => all.Where(relation => relation.SupportsContainedByFor(t)).ToImmutableArray(),
                _allRelations);
        }
    }
}
