// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public class DependencyExtensionsTests
    {
        [Fact]
        public void IsOrHasUnresolvedDependency()
        {
            var dependency1 = new TestDependency
            {
                ProviderType = "Yyy",
                Id = "tfm1\\yyy\\dependencyExisting",
                Name = "dependencyExisting",
                Caption = "DependencyExisting",
                Resolved = false
            };

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.Implement();

            Assert.True(dependency1.IsOrHasUnresolvedDependency(mockSnapshot));

            var dependency2 = new TestDependency
            {
                ClonePropertiesFrom = dependency1,
                Resolved = true
            };

            mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency("tfm1\\yyy\\dependencyExisting", true);

            Assert.True(dependency2.IsOrHasUnresolvedDependency(mockSnapshot));

            mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementHasUnresolvedDependency("tfm1\\yyy\\dependencyExisting", false);

            Assert.False(dependency2.IsOrHasUnresolvedDependency(mockSnapshot));
        }

        [Fact]
        public void ToViewModel()
        {
            var iconSet = new DependencyIconSet(
                KnownMonikers.Uninstall,
                KnownMonikers.AbsolutePosition,
                KnownMonikers.AboutBox,
                KnownMonikers.Abbreviation);

            var dependencyResolved = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Caption"":""DependencyExisting"",
    ""HasUnresolvedDependency"":""false"",
    ""SchemaName"":""MySchema"",
    ""SchemaItemType"":""MySchemaItemType"",
    ""Priority"":""1"",
    ""Resolved"":""true""
}", iconSet: iconSet);

            var dependencyUnresolved = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Yyy"",
    ""Id"": ""tfm1\\yyy\\dependencyExisting"",
    ""Name"":""dependencyExisting"",
    ""Caption"":""DependencyExisting"",
    ""HasUnresolvedDependency"":""false"",
    ""SchemaName"":""MySchema"",
    ""SchemaItemType"":""MySchemaItemType"",
    ""Priority"":""1"",
    ""Resolved"":""false""
}", iconSet: iconSet);

            var mockSnapshot = ITargetedDependenciesSnapshotFactory.ImplementMock(checkForUnresolvedDependencies: false).Object;
            var mockSnapshotUnresolvedDependency = ITargetedDependenciesSnapshotFactory.ImplementMock(checkForUnresolvedDependencies: true).Object;

            var viewModelResolved = dependencyResolved.ToViewModel(mockSnapshot);

            Assert.Equal(dependencyResolved.Caption, viewModelResolved.Caption);
            Assert.Equal(dependencyResolved.Flags, viewModelResolved.Flags);
            Assert.Equal(dependencyResolved.Id, viewModelResolved.FilePath);
            Assert.Equal(dependencyResolved.SchemaName, viewModelResolved.SchemaName);
            Assert.Equal(dependencyResolved.SchemaItemType, viewModelResolved.SchemaItemType);
            Assert.Equal(dependencyResolved.Priority, viewModelResolved.Priority);
            Assert.Equal(iconSet.Icon, viewModelResolved.Icon);
            Assert.Equal(iconSet.ExpandedIcon, viewModelResolved.ExpandedIcon);

            var viewModelUnresolved = dependencyUnresolved.ToViewModel(mockSnapshot);

            Assert.Equal(dependencyUnresolved.Caption, viewModelUnresolved.Caption);
            Assert.Equal(dependencyUnresolved.Flags, viewModelUnresolved.Flags);
            Assert.Equal(dependencyUnresolved.Id, viewModelUnresolved.FilePath);
            Assert.Equal(dependencyUnresolved.SchemaName, viewModelUnresolved.SchemaName);
            Assert.Equal(dependencyUnresolved.SchemaItemType, viewModelUnresolved.SchemaItemType);
            Assert.Equal(dependencyUnresolved.Priority, viewModelUnresolved.Priority);
            Assert.Equal(iconSet.UnresolvedIcon, viewModelUnresolved.Icon);
            Assert.Equal(iconSet.UnresolvedExpandedIcon, viewModelUnresolved.ExpandedIcon);

            var viewModelUnresolvedDependency = dependencyResolved.ToViewModel(mockSnapshotUnresolvedDependency);

            Assert.Equal(dependencyUnresolved.Caption, viewModelUnresolvedDependency.Caption);
            Assert.Equal(dependencyUnresolved.Flags, viewModelUnresolvedDependency.Flags);
            Assert.Equal(dependencyUnresolved.Id, viewModelUnresolvedDependency.FilePath);
            Assert.Equal(dependencyUnresolved.SchemaName, viewModelUnresolvedDependency.SchemaName);
            Assert.Equal(dependencyUnresolved.SchemaItemType, viewModelUnresolvedDependency.SchemaItemType);
            Assert.Equal(dependencyUnresolved.Priority, viewModelUnresolvedDependency.Priority);
            Assert.Equal(iconSet.UnresolvedIcon, viewModelUnresolvedDependency.Icon);
            Assert.Equal(iconSet.UnresolvedExpandedIcon, viewModelUnresolvedDependency.ExpandedIcon);
        }

        [Fact]
        public void IsPackage()
        {
            var dependency = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""NuGetDependency""
}");

            Assert.True(dependency.IsPackage());

            var dependency2 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""Project""
}");

            Assert.False(dependency2.IsPackage());
        }

        [Fact]
        public void IsProject()
        {
            var dependency = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""ProjectDependency""
}");

            Assert.True(dependency.IsProject());

            var dependency2 = IDependencyFactory.FromJson(@"
{
    ""ProviderType"": ""NotProject""
}");

            Assert.False(dependency2.IsProject());
        }

        [Fact]
        public void HasSameTarget()
        {
            var targetFramework1 = ITargetFrameworkFactory.Implement("tfm1");
            var targetFramework2 = ITargetFrameworkFactory.Implement("tfm2");

            var dependency1 = IDependencyFactory.Implement(targetFramework: targetFramework1).Object;
            var dependency2 = IDependencyFactory.Implement(targetFramework: targetFramework1).Object;
            var dependency3 = IDependencyFactory.Implement(targetFramework: targetFramework2).Object;

            Assert.True(dependency1.HasSameTarget(dependency2));
            Assert.False(dependency1.HasSameTarget(dependency3));
        }

        [Fact]
        public void GetTopLevelId()
        {
            var dependency1 = IDependencyFactory.FromJson(@"
{
    ""Id"":""id1"",
    ""ProviderType"": ""ProjectDependency""
}");

            Assert.Equal("id1", dependency1.GetTopLevelId());

            var dependency2 = IDependencyFactory.FromJson(@"
{
    ""Id"":""id1"",
    ""Path"":""xxxxxxx"",
    ""ProviderType"": ""MyProvider""
}", targetFramework: ITargetFrameworkFactory.Implement("tfm1"));

            Assert.Equal("tfm1\\MyProvider\\xxxxxxx", dependency2.GetTopLevelId());
        }
    }
}
