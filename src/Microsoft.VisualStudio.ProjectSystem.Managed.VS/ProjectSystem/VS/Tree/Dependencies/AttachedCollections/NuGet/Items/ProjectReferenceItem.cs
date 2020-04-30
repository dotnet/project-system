// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for transitive project reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class ProjectReferenceItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; private set; }
        public AssetsFileTargetLibrary Library { get; private set; }

        public ProjectReferenceItem(AssetsFileTarget target, AssetsFileTargetLibrary library)
            : base(library.Name)
        {
            Target = target;
            Library = library;
        }

        internal bool TryUpdateState(AssetsFileTarget target, AssetsFileTargetLibrary library)
        {
            if (ReferenceEquals(Target, target) && ReferenceEquals(Library, library))
            {
                return false;
            }

            Target = target;
            Library = library;
            return true;
        }

        public override object Identity => Library.Name;

        public override int Priority => AttachedItemPriority.Project;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.Application;

        protected override bool TryGetProjectNode(IProjectTree targetRootNode, IRelatableItem item, [NotNullWhen(returnValue: true)] out IProjectTree? projectTree)
        {
            IProjectTree? typeGroupNode = targetRootNode.FindChildWithFlags(DependencyTreeFlags.ProjectDependencyGroup);

            projectTree = typeGroupNode?.FindChildWithFlags(ProjectTreeFlags.Create("$ID:" + Library.Name));

            return projectTree != null;
        }

        public override object? GetBrowseObject() => new BrowseObject(Library);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly AssetsFileTargetLibrary _library;

            public BrowseObject(AssetsFileTargetLibrary library) => _library = library;

            public override string GetComponentName() => _library.Name;

            public override string GetClassName() => VSResources.ProjectReferenceBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.ProjectReferenceNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.ProjectReferenceNameDescription))]
            public string Name => _library.Name;
        }
    }
}
