// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.NuGet
{
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(ProjectReferenceAttachedCollectionSourceProvider))]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class ProjectReferenceAttachedCollectionSourceProvider : AssetsFileTopLevelDependenciesCollectionSourceProvider<string, ProjectReferenceItem>
    {
        [ImportingConstructor]
        public ProjectReferenceAttachedCollectionSourceProvider(JoinableTaskContext joinableTaskContext)
            : base(DependencyTreeFlags.ProjectDependency, joinableTaskContext)
        {
        }

        protected override bool TryGetIdentity(string flagsString, out string identity)
        {
            if (IVsHierarchyItemExtensions.TryGetProjectDetails(flagsString, out string? projectId))
            {
                identity = projectId;
                return true;
            }

            identity = null!;
            return false;
        }

        protected override bool TryGetLibrary(AssetsFileTarget target, string identity, [NotNullWhen(returnValue: true)] out AssetsFileTargetLibrary? library)
        {
            return target.TryGetProject(identity, out library);
        }

        protected override ProjectReferenceItem CreateItem(AssetsFileTarget targetData, AssetsFileTargetLibrary library)
        {
            return new ProjectReferenceItem(targetData, library);
        }

        protected override bool TryUpdateItem(ProjectReferenceItem item, AssetsFileTarget targetData, AssetsFileTargetLibrary library)
        {
            return item.TryUpdateState(targetData, library);
        }
    }
}
