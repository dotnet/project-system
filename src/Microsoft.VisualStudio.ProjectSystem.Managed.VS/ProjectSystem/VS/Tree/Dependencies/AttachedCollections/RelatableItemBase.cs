// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Base class for <see cref="IRelatableItem"/> implementations. Derives from <see cref="AttachedCollectionItemBase"/>
    /// to include common patterns for attached items.
    /// </summary>
    public abstract partial class RelatableItemBase : AttachedCollectionItemBase, IRelatableItem
    {
        private const int IDM_VS_CTXT_DEPENDENCY_TRANSITIVE_ITEM = 0x04B0;

        private static readonly IContextMenuController s_defaultMenuController = new MenuController(VsMenus.guidSHLMainMenu, IDM_VS_CTXT_DEPENDENCY_TRANSITIVE_ITEM);

        private AggregateContainsRelationCollection? _containsCollection;

        protected RelatableItemBase(string name)
            : base(name)
        {
        }

        public abstract object Identity { get; }

        protected override IContextMenuController? ContextMenuController => s_defaultMenuController;

        AggregateContainsRelationCollection? IRelatableItem.ContainsCollection => _containsCollection;

        AggregateContainedByRelationCollection? IRelatableItem.ContainedByCollection { get; set; }

        bool IRelatableItem.TryGetOrCreateContainsCollection(
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateContainsRelationCollection? relationCollection)
        {
            if (_containsCollection is null && AggregateContainsRelationCollection.TryCreate(this, relationProvider, out AggregateContainsRelationCollection? collection))
            {
                _containsCollection = collection;
            }

            relationCollection = _containsCollection;
            return relationCollection is not null;
        }

        bool IRelatableItem.TryGetProjectNode(IProjectTree targetRootNode, IRelatableItem item, [NotNullWhen(returnValue: true)] out IProjectTree? projectTree)
        {
            return TryGetProjectNode(targetRootNode, item, out projectTree);
        }

        /// <inheritdoc cref="IRelatableItem.TryGetProjectNode" />
        protected virtual bool TryGetProjectNode(IProjectTree targetRootNode, IRelatableItem item, [NotNullWhen(returnValue: true)] out IProjectTree? projectTree)
        {
            projectTree = null;
            return false;
        }
    }
}
