// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    [Export(typeof(IRelation))]
    internal sealed class FrameworkToFrameworkAssemblyRelation : RelationBase<FrameworkReferenceItem, FrameworkReferenceAssemblyItem>
    {
        private readonly ITargetFrameworkContentCache _frameworkContentCache;

        [ImportingConstructor]
        public FrameworkToFrameworkAssemblyRelation(ITargetFrameworkContentCache frameworkContentCache)
        {
            _frameworkContentCache = frameworkContentCache;
        }

        protected override bool HasContainedItems(FrameworkReferenceItem parent)
        {
            return !_frameworkContentCache.GetContents(parent.Framework).IsEmpty;
        }

        protected override void UpdateContainsCollection(FrameworkReferenceItem parent, AggregateContainsRelationCollectionSpan span)
        {
            ImmutableArray<FrameworkReferenceAssemblyItem> assemblies = _frameworkContentCache.GetContents(parent.Framework);

            span.UpdateContainsItems(
                assemblies.OrderBy(assembly => assembly.Text),
                (sourceItem, targetItem) => StringComparer.Ordinal.Compare(sourceItem.Text, targetItem.Text),
                (sourceItem, targetItem) => false,
                sourceItem => sourceItem);
        }

        protected override IEnumerable<FrameworkReferenceItem>? CreateContainedByItems(FrameworkReferenceAssemblyItem child)
        {
            yield return new FrameworkReferenceItem(child.Framework);
        }
    }
}
