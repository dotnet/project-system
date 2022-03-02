// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Typed wrapper for <see cref="IRelation"/> that uses specific types for parent/child items.
    /// </summary>
    /// <remarks>
    /// Using this type makes it easier to implement <see cref="IRelation"/> correctly.
    /// </remarks>
    /// <typeparam name="TParent">The type of <see cref="IRelatableItem"/> parents in the relation.</typeparam>
    /// <typeparam name="TChild">The type of <see cref="IRelatableItem"/> children in the relation.</typeparam>
    public abstract class RelationBase<TParent, TChild> : IRelation where TParent : class, IRelatableItem
        where TChild : class, IRelatableItem
    {
        void IRelation.UpdateContainsCollection(IRelatableItem parent, AggregateContainsRelationCollectionSpan span)
        {
            if (parent is TParent typedParent)
            {
                UpdateContainsCollection(typedParent, span);
            }
        }

        IEnumerable<IRelatableItem>? IRelation.CreateContainedByItems(IRelatableItem child)
        {
            if (child is TChild typedChild)
            {
                return CreateContainedByItems(typedChild);
            }

            return null;
        }

        bool IRelation.SupportsContainsFor(Type parentType) => ReferenceEquals(parentType, typeof(TParent));

        bool IRelation.SupportsContainedByFor(Type childType) => ReferenceEquals(childType, typeof(TChild));

        bool IRelation.HasContainedItem(IRelatableItem parent) => HasContainedItems((TParent)parent);

        /// <inheritdoc cref="IRelation.HasContainedItem"/>
        protected abstract bool HasContainedItems(TParent parent);

        /// <inheritdoc cref="IRelation.UpdateContainsCollection"/>
        protected abstract void UpdateContainsCollection(TParent parent, AggregateContainsRelationCollectionSpan span);

        /// <inheritdoc cref="IRelation.CreateContainedByItems"/>
        protected abstract IEnumerable<TParent>? CreateContainedByItems(TChild child);
    }
}
