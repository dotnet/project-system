// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for named group of assemblies within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items within this group have type <see cref="PackageAssemblyItem"/>.
    /// </remarks>
    internal sealed class PackageAssemblyGroupItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; }
        public AssetsFileTargetLibrary Library { get; }
        public PackageAssemblyGroupType GroupType { get; }

        public PackageAssemblyGroupItem(AssetsFileTarget target, AssetsFileTargetLibrary library, PackageAssemblyGroupType groupType)
            : base(GetGroupLabel(groupType))
        {
            Target = target;
            Library = library;
            GroupType = groupType;
        }

        private static string GetGroupLabel(PackageAssemblyGroupType groupType)
        {
            return groupType switch
            {
                PackageAssemblyGroupType.CompileTime => VSResources.PackageCompileTimeAssemblyGroupName,
                PackageAssemblyGroupType.Framework => VSResources.PackageFrameworkAssemblyGroupName,
                _ => throw new InvalidEnumArgumentException(nameof(groupType), (int)groupType, typeof(PackageAssemblyGroupType))
            };
        }

        public override object Identity => Tuple.Create(GroupType, Library.Name);

        public override int Priority => GroupType switch
        {
            PackageAssemblyGroupType.CompileTime => AttachedItemPriority.CompileTimeAssemblyGroup,
            PackageAssemblyGroupType.Framework => AttachedItemPriority.FrameworkAssemblyGroup,
            _ => throw new InvalidEnumArgumentException(nameof(GroupType), (int)GroupType, typeof(PackageAssemblyGroupType))
        };

        public override ImageMoniker IconMoniker => ManagedImageMonikers.ReferenceGroup;

    }
}
