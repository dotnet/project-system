// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    public class DependencyExtensionsTests
    {
        [Fact]
        public void ToViewModel()
        {
            var iconSet = new DependencyIconSet(
                KnownMonikers.Uninstall,
                KnownMonikers.AbsolutePosition,
                KnownMonikers.AboutBox,
                KnownMonikers.Abbreviation);

            var dependencyResolved = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyResolved",
                Name = "dependencyResolved",
                Caption = "DependencyResolved",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Resolved = true,
                IconSet = iconSet
            };

            var dependencyUnresolved = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyUnresolved",
                Name = "dependencyUnresolved",
                Caption = "DependencyUnresolved",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Resolved = false,
                IconSet = iconSet
            };

            var viewModelResolved = dependencyResolved.ToViewModel();

            Assert.Equal(dependencyResolved.Caption,               viewModelResolved.Caption);
            Assert.Equal(dependencyResolved.Flags,                 viewModelResolved.Flags);
            Assert.Equal(dependencyResolved.SchemaName,            viewModelResolved.SchemaName);
            Assert.Equal(dependencyResolved.SchemaItemType,        viewModelResolved.SchemaItemType);
            Assert.Equal(iconSet.Icon,                             viewModelResolved.Icon);
            Assert.Equal(iconSet.ExpandedIcon,                     viewModelResolved.ExpandedIcon);

            var viewModelUnresolved = dependencyUnresolved.ToViewModel();

            Assert.Equal(dependencyUnresolved.Caption,             viewModelUnresolved.Caption);
            Assert.Equal(dependencyUnresolved.Flags,               viewModelUnresolved.Flags);
            Assert.Equal(dependencyUnresolved.SchemaName,          viewModelUnresolved.SchemaName);
            Assert.Equal(dependencyUnresolved.SchemaItemType,      viewModelUnresolved.SchemaItemType);
            Assert.Equal(iconSet.UnresolvedIcon,                   viewModelUnresolved.Icon);
            Assert.Equal(iconSet.UnresolvedExpandedIcon,           viewModelUnresolved.ExpandedIcon);
        }
    }
}
