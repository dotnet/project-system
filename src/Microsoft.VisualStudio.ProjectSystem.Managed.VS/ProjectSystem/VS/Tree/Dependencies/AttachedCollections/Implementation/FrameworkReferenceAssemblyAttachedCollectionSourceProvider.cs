// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    [AppliesToProject(ProjectCapability.DependenciesTree)]
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(FrameworkReferenceAssemblyAttachedCollectionSourceProvider))]
    [VisualStudio.Utilities.Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class FrameworkReferenceAssemblyAttachedCollectionSourceProvider : DependenciesAttachedCollectionSourceProviderBase
    {
        private readonly IRelationProvider _relationProvider;

        [ImportingConstructor]
        public FrameworkReferenceAssemblyAttachedCollectionSourceProvider(IRelationProvider relationProvider)
            : base(DependencyTreeFlags.FrameworkDependency)
        {
            _relationProvider = relationProvider;
        }

        protected override bool TryCreateCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string flagsString,
            string? target,
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateRelationCollectionSource? containsCollectionSource)
        {
            if (ErrorHandler.Succeeded(hierarchyItem.HierarchyIdentity.Hierarchy.GetProperty(
                hierarchyItem.HierarchyIdentity.ItemID, (int)__VSHPROPID.VSHPROPID_ExtObject, out object projectItemObject)))
            {
                var projectItem = projectItemObject as ProjectItem;
                EnvDTE.Properties? props = projectItem?.Properties;

                if (props?.Item("TargetingPackPath")?.Value is string path &&
                    props?.Item("OriginalItemSpec")?.Value is string name &&
                    !string.IsNullOrWhiteSpace(path) &&
                    !string.IsNullOrWhiteSpace(name))
                {
                    string? profile = props?.Item("Profile").Value as string;

                    var framework = new FrameworkReferenceIdentity(path, profile, name);
                    var item = new FrameworkReferenceItem(framework);
                    if (AggregateContainsRelationCollection.TryCreate(item, _relationProvider, out AggregateContainsRelationCollection? collection))
                    {
                        containsCollectionSource = new AggregateRelationCollectionSource(hierarchyItem, collection);
                        return true;
                    }
                }
            }

            containsCollectionSource = null;
            return false;
        }
    }
}
