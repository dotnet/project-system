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
    [Name(nameof(PackageReferenceAttachedCollectionSourceProvider))]
    [Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class PackageReferenceAttachedCollectionSourceProvider : AssetsFileTopLevelDependenciesCollectionSourceProvider<(string Name, string Version), PackageReferenceItem>
    {
        [ImportingConstructor]
        public PackageReferenceAttachedCollectionSourceProvider(JoinableTaskContext joinableTaskContext)
            : base(DependencyTreeFlags.PackageDependency, joinableTaskContext)
        {
        }

        protected override bool TryGetIdentity(string flagsString, out (string Name, string Version) identity)
        {
            if (IVsHierarchyItemExtensions.TryGetPackageDetails(flagsString, out string? packageId, out string? packageVersion))
            {
                identity = (packageId, packageVersion);
                return true;
            }

            identity = default;
            return false;
        }

        protected override bool TryGetLibrary(AssetsFileTarget target, (string Name, string Version) identity, [NotNullWhen(returnValue: true)] out AssetsFileTargetLibrary? library)
        {
            return target.TryGetPackage(identity.Name, identity.Version, out library);
        }

        protected override PackageReferenceItem CreateItem(AssetsFileTarget targetData, AssetsFileTargetLibrary library)
        {
            return new PackageReferenceItem(targetData, library);
        }

        protected override bool TryUpdateItem(PackageReferenceItem item, AssetsFileTarget targetData, AssetsFileTargetLibrary library)
        {
            return item.TryUpdateState(targetData, library);
        }
    }
}
