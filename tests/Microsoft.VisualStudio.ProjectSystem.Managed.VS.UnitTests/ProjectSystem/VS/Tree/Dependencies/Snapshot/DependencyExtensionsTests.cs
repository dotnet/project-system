// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
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

            // This is a resolved dependency
            var dependencyResolved = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyResolved",
                Name = "dependencyResolved",
                Caption = "DependencyResolved",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Priority = 1,
                Resolved = true,
                TopLevel = true,
                IconSet = iconSet
            };

            // This is an unresolved dependency
            var dependencyUnresolved = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyUnresolved",
                Name = "dependencyUnresolved",
                Caption = "DependencyUnresolved",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Priority = 1,
                Resolved = false,
                TopLevel = true,
                IconSet = iconSet
            };

            // This is a resolved dependency with an unresolved child dependency
            var dependencyUnresolvedChild = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyUnresolvedChild",
                Name = "dependencyUnresolvedChild",
                Caption = "DependencyUnresolvedChild",
                SchemaName = "MySchema",
                SchemaItemType = "MySchemaItemType",
                Priority = 1,
                Resolved = true,
                TopLevel = true,
                IconSet = iconSet,
                DependencyIDs = ImmutableArray.Create(dependencyUnresolved.Id)
            };

            var snapshotResolved        = TargetedDependenciesSnapshotFactory.ImplementFromDependencies(new[] { dependencyResolved });
            var snapshotUnresolved      = TargetedDependenciesSnapshotFactory.ImplementFromDependencies(new[] { dependencyUnresolved });
            var snapshotUnresolvedChild = TargetedDependenciesSnapshotFactory.ImplementFromDependencies(new[] { dependencyUnresolved, dependencyUnresolvedChild });

            Assert.False(snapshotResolved.ShouldAppearUnresolved(dependencyResolved));
            Assert.True(snapshotUnresolved.ShouldAppearUnresolved(dependencyUnresolved));
            Assert.True(snapshotUnresolvedChild.ShouldAppearUnresolved(dependencyUnresolvedChild));

            var viewModelResolved = dependencyResolved.ToViewModel(snapshotResolved);

            Assert.Equal(dependencyResolved.Caption,               viewModelResolved.Caption);
            Assert.Equal(dependencyResolved.Flags,                 viewModelResolved.Flags);
            Assert.Equal(dependencyResolved.Id,                    viewModelResolved.FilePath);
            Assert.Equal(dependencyResolved.SchemaName,            viewModelResolved.SchemaName);
            Assert.Equal(dependencyResolved.SchemaItemType,        viewModelResolved.SchemaItemType);
            Assert.Equal(dependencyResolved.Priority,              viewModelResolved.Priority);
            Assert.Equal(iconSet.Icon,                             viewModelResolved.Icon);
            Assert.Equal(iconSet.ExpandedIcon,                     viewModelResolved.ExpandedIcon);

            var viewModelUnresolved = dependencyUnresolved.ToViewModel(snapshotResolved);

            Assert.Equal(dependencyUnresolved.Caption,             viewModelUnresolved.Caption);
            Assert.Equal(dependencyUnresolved.Flags,               viewModelUnresolved.Flags);
            Assert.Equal(dependencyUnresolved.Id,                  viewModelUnresolved.FilePath);
            Assert.Equal(dependencyUnresolved.SchemaName,          viewModelUnresolved.SchemaName);
            Assert.Equal(dependencyUnresolved.SchemaItemType,      viewModelUnresolved.SchemaItemType);
            Assert.Equal(dependencyUnresolved.Priority,            viewModelUnresolved.Priority);
            Assert.Equal(iconSet.UnresolvedIcon,                   viewModelUnresolved.Icon);
            Assert.Equal(iconSet.UnresolvedExpandedIcon,           viewModelUnresolved.ExpandedIcon);

            var viewModelUnresolvedChild = dependencyUnresolvedChild.ToViewModel(snapshotUnresolvedChild);

            Assert.Equal(dependencyUnresolvedChild.Caption,        viewModelUnresolvedChild.Caption);
            Assert.Equal(dependencyUnresolvedChild.Flags,          viewModelUnresolvedChild.Flags);
            Assert.Equal(dependencyUnresolvedChild.Id,             viewModelUnresolvedChild.FilePath);
            Assert.Equal(dependencyUnresolvedChild.SchemaName,     viewModelUnresolvedChild.SchemaName);
            Assert.Equal(dependencyUnresolvedChild.SchemaItemType, viewModelUnresolvedChild.SchemaItemType);
            Assert.Equal(dependencyUnresolvedChild.Priority,       viewModelUnresolvedChild.Priority);
            Assert.Equal(iconSet.UnresolvedIcon,                   viewModelUnresolvedChild.Icon);
            Assert.Equal(iconSet.UnresolvedExpandedIcon,           viewModelUnresolvedChild.ExpandedIcon);
        }

        [Theory]
        [InlineData(AnalyzerRuleHandler.ProviderTypeString,  false)]
        [InlineData(AssemblyRuleHandler.ProviderTypeString,  false)]
        [InlineData(ComRuleHandler.ProviderTypeString,       false)]
        [InlineData(FrameworkRuleHandler.ProviderTypeString, false)]
        [InlineData(PackageRuleHandler.ProviderTypeString,   true)]
        [InlineData(ProjectRuleHandler.ProviderTypeString,   false)]
        [InlineData(SdkRuleHandler.ProviderTypeString,       false)]
        public void IsPackage(string providerType, bool isPackage)
        {
            var dependency = new TestDependency
            {
                ProviderType = providerType
            };

            Assert.Equal(isPackage, dependency.IsPackage());
        }

        [Theory]
        [InlineData(AnalyzerRuleHandler.ProviderTypeString,  false)]
        [InlineData(AssemblyRuleHandler.ProviderTypeString,  false)]
        [InlineData(ComRuleHandler.ProviderTypeString,       false)]
        [InlineData(FrameworkRuleHandler.ProviderTypeString, false)]
        [InlineData(PackageRuleHandler.ProviderTypeString,   false)]
        [InlineData(ProjectRuleHandler.ProviderTypeString,   true)]
        [InlineData(SdkRuleHandler.ProviderTypeString,       false)]
        public void IsProject(string providerType, bool isProject)
        {
            var dependency = new TestDependency
            {
                ProviderType = providerType
            };

            Assert.Equal(isProject, dependency.IsProject());
        }

        [Fact]
        public void HasSameTarget()
        {
            var targetFramework1 = new TargetFramework("tfm1");
            var targetFramework2 = new TargetFramework("tfm2");

            var dependency1 = new TestDependency { TargetFramework = targetFramework1 };
            var dependency2 = new TestDependency { TargetFramework = targetFramework1 };
            var dependency3 = new TestDependency { TargetFramework = targetFramework2 };

            Assert.True(dependency1.HasSameTarget(dependency2));
            Assert.False(dependency1.HasSameTarget(dependency3));
        }

        [Fact]
        public void GetTopLevelId()
        {
            var dependency1 = new TestDependency
            {
                Id = "id1",
                ProviderType = "MyProvider"
            };

            Assert.Equal("id1", dependency1.GetTopLevelId());

            var dependency2 = new TestDependency
            {
                Id = "id1",
                Path = "xxxxxxx",
                ProviderType = "MyProvider",
                TargetFramework = new TargetFramework("tfm1")
            };

            Assert.Equal("tfm1\\MyProvider\\xxxxxxx", dependency2.GetTopLevelId());
        }
    }
}
