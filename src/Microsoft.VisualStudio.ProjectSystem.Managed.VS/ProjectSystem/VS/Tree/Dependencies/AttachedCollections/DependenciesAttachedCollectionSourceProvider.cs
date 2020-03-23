// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Identifies which hierarchy items in the "Dependencies" tree should have collections of child
    /// items attached, and provides source objects for those collections.
    /// </summary>
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(DependenciesAttachedCollectionSourceProvider))]
    [VisualStudio.Utilities.Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed partial class DependenciesAttachedCollectionSourceProvider : IAttachedCollectionSourceProvider
    {
        [ImportMany]
        public OrderPrecedenceImportCollection<IDependenciesTreeAttachedCollectionSourceProvider> Providers { get; }

        public DependenciesAttachedCollectionSourceProvider()
        {
            Providers = new OrderPrecedenceImportCollection<IDependenciesTreeAttachedCollectionSourceProvider>(projectCapabilityCheckProvider: (UnconfiguredProject?)null);
        }

        public IAttachedCollectionSource? CreateCollectionSource(object item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                if (item is IVsHierarchyItem hierarchyItem)
                {
                    if (hierarchyItem.TryGetFlagsString(out string? flagsString))
                    {
                        // TODO only call providers for top-level dependencies
                        foreach (Lazy<IDependenciesTreeAttachedCollectionSourceProvider, IOrderPrecedenceMetadataView> provider in Providers)
                        {
                            if (provider.Value.FlagsDetector.Matches(flagsString))
                            {
                                return provider.Value.TryCreateSource(hierarchyItem);
                            }
                        }
                    }
                }
                else if (item is IAttachedCollectionSource source)
                {
                    // Tree items which are themselves sources can be returned here
                    return source;
                }
            }

            return null;
        }

        public IEnumerable<IAttachedRelationship> GetRelationships(object item) => Enumerable.Empty<IAttachedRelationship>();
    }
}
