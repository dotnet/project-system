// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class LaunchProfilesProjectItemProviderTests
    {
        [Fact]
        public async Task GetItemTypesAsync_ReturnsLaunchProfile()
        {
            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                ILaunchSettingsProviderFactory.Create());

            var itemTypes = await itemProvider.GetItemTypesAsync();

            Assert.Single(itemTypes, LaunchProfileProjectItemProvider.ItemType);
        }

        [Fact]
        public async Task WhenThereAreNoLaunchProfiles_GetExistingItemTypesAsyncReturnsAnEmptySet()
        {
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new LaunchProfile[0]);

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var existingItemTypes = await itemProvider.GetExistingItemTypesAsync();

            Assert.Empty(existingItemTypes);
        }

        [Fact]
        public async Task WhenThereAreLaunchProfiles_GetExistingItemTypesAsyncReturnsASingleItem()
        {
            var profile = new WritableLaunchProfile { Name = "Test" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var existingItemTypes = await itemProvider.GetExistingItemTypesAsync();

            Assert.Single(existingItemTypes, LaunchProfileProjectItemProvider.ItemType);
        }

        [Fact]
        public async Task WhenThereAreNoLaunchProfiles_GetItemsAsyncReturnsAnEmptyEnumerable()
        {
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new LaunchProfile[0]);

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var items = await itemProvider.GetItemsAsync();

            Assert.Empty(items);
        }

        [Fact]
        public async Task WhenThereAreLaunchProfiles_GetItemsAsyncReturnsAnItemForEachProfile()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var items = await itemProvider.GetItemsAsync();

            Assert.Collection(items,
                item => Assert.Equal("Profile1", item.EvaluatedInclude),
                item => Assert.Equal("Profile2", item.EvaluatedInclude));
        }

        [Fact]
        public async Task WhenAskedForLaunchProfileItemTypes_GetItemsAsyncReturnsAnItemForEachProfile()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var items = await itemProvider.GetItemsAsync(LaunchProfileProjectItemProvider.ItemType);

            Assert.Collection(items,
                item => Assert.Equal("Profile1", item.EvaluatedInclude),
                item => Assert.Equal("Profile2", item.EvaluatedInclude));

            items = await itemProvider.GetItemsAsync(LaunchProfileProjectItemProvider.ItemType, "Profile2");

            Assert.Collection(items,
                item => Assert.Equal("Profile2", item.EvaluatedInclude));
        }

        [Fact]
        public async Task WhenAskedForOtherItemTypes_GetItemsAsyncReturnsAnEmptyEnumerable()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var items = await itemProvider.GetItemsAsync("RandomItemType");

            Assert.Empty(items);

            items = await itemProvider.GetItemsAsync("RandomItemType", "Profile2");

            Assert.Empty(items);
        }

        [Fact]
        public async Task WhenProjectPropertyContextHasNoItemType_GetItemsAsyncReturnsAnItemWithAMatchingName()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var context = new TestProjectPropertiesContext(
                isProjectFile: true,
                file: "Foo",
                itemType: null,
                itemName: "Profile2");

            var item = await itemProvider.GetItemAsync(context);

            Assert.NotNull(item);
            Assert.Equal("Profile2", item.EvaluatedInclude);
        }

        [Fact]
        public async Task WhenProjectPropertyContextHasTheWrongItemType_GetItemsAsyncReturnsNull()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var context = new TestProjectPropertiesContext(
                isProjectFile: true,
                file: "Foo",
                itemType: "RandomItemType",
                itemName: "Profile2");

            var item = await itemProvider.GetItemAsync(context);

            Assert.Null(item);
        }

        [Fact]
        public async Task WhenProjectPropertyContextHasLaunchProfileItemType_GetItemsAsyncReturnsAnItemWithAMatchingName()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var context = new TestProjectPropertiesContext(
                isProjectFile: true,
                file: "Foo",
                itemType: LaunchProfileProjectItemProvider.ItemType,
                itemName: "Profile2");

            var item = await itemProvider.GetItemAsync(context);

            Assert.NotNull(item);
            Assert.Equal("Profile2", item.EvaluatedInclude);
        }

        [Fact]
        public async Task WhenGivenMultipleProjectPropertyContexts_GetItemsAsyncReturnsNullOrAnItemForEach()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var profile3 = new WritableLaunchProfile { Name = "Profile3" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[]
                {
                    profile1.ToLaunchProfile(),
                    profile2.ToLaunchProfile(),
                    profile3.ToLaunchProfile()
                });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            List<IProjectPropertiesContext> contexts = new()
            {
                new TestProjectPropertiesContext(true, "Foo", null, "Profile3"),
                new TestProjectPropertiesContext(true, "Foo", "RandomItemType", "Profile2"),
                new TestProjectPropertiesContext(true, "Foo", LaunchProfileProjectItemProvider.ItemType, "Profile1")
            };

            var items = await itemProvider.GetItemsAsync(contexts);

            Assert.Collection(items,
                item => Assert.Equal("Profile3", item!.EvaluatedInclude),
                Assert.Null,
                item => Assert.Equal("Profile1", item!.EvaluatedInclude));
        }

        [Fact]
        public async Task WhenFindingAnItemByName_TheMatchingItemIsReturnedIfItExists()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var item = await itemProvider.FindItemByNameAsync("Profile2");

            Assert.NotNull(item);
            Assert.Equal(expected: "Profile2", actual: item.EvaluatedInclude);
        }

        [Fact]
        public async Task WhenFindingAnItemByName_NullIsReturnedIfNoMatchingItemExists()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var item = await itemProvider.FindItemByNameAsync("Profile3");

            Assert.Null(item);
        }

        [Fact]
        public async Task WhenAddingAnItem_AnExceptionIsThrownIfTheItemTypeIsWrong()
        {
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create();

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var item = await itemProvider.AddAsync(itemType: "RandomItemType", include: "Alpha Profile");
            });
        }

        [Fact(Skip = "Item metadata has not yet been implemented.")]
        public Task WhenAddingAnItem_TheReturnedItemHasAllTheExpectedMetadata()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task WhenAddingAnItem_TheReturnedItemHasTheCorrectName()
        {
            ILaunchProfile? newProfile = null;
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (p, a) =>
                {
                    newProfile = p;
                },
                getProfilesCallback: initialProfiles =>
                {
                    Assert.NotNull(newProfile);
                    return initialProfiles.Add(newProfile);
                });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var item = await itemProvider.AddAsync(itemType: LaunchProfileProjectItemProvider.ItemType, include: "Alpha Profile");

            Assert.Equal(expected: "Alpha Profile", actual: item!.EvaluatedInclude);
            Assert.Equal(expected: "Alpha Profile", actual: item!.UnevaluatedInclude);
        }

        [Fact]
        public async Task WhenAddingMultipleItems_TheReturnedItemsHaveTheCorrectNames()
        {
            ImmutableList<ILaunchProfile> newProfiles = ImmutableList<ILaunchProfile>.Empty; ;
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (p, a) =>
                {
                    newProfiles = newProfiles.Add(p);
                },
                getProfilesCallback: initialProfiles =>
                {
                    return initialProfiles.AddRange(newProfiles);
                });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var items = await itemProvider.AddAsync(
                new Tuple<string, string, IEnumerable<KeyValuePair<string, string>>?>[]
                {
                    new(LaunchProfileProjectItemProvider.ItemType, "Alpha Profile", null),
                    new(LaunchProfileProjectItemProvider.ItemType, "Beta Profile", null),
                    new(LaunchProfileProjectItemProvider.ItemType, "Gamma Profile", null),

                });

            Assert.Collection(items,
                item => { Assert.Equal("Alpha Profile", item.EvaluatedInclude); Assert.Equal("Alpha Profile", item.UnevaluatedInclude); },
                item => { Assert.Equal("Beta Profile", item.EvaluatedInclude); Assert.Equal("Beta Profile", item.UnevaluatedInclude); },
                item => { Assert.Equal("Gamma Profile", item.EvaluatedInclude); Assert.Equal("Gamma Profile", item.UnevaluatedInclude); });
        }

        [Fact]
        public async Task WhenAddingAnItemAsAPath_AnInvalidOperationExceptionIsThrown()
        {
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create();
            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await itemProvider.AddAsync(@"C:\alpha\beta\gamma\delta.fakeextension");
            });
        }

        [Fact]
        public async Task WhenAddingItemsAsPaths_AnInvalidOperationExceptionIsThrown()
        {
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create();
            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await itemProvider.AddAsync(new[]
                {
                    @"C:\alpha\beta\gamma\delta.fakeextension",
                    @"C:\epsilon\phi\kappa\psi.fakeextension"
                });
            });
        }

        [Fact]
        public async Task WhenRenamingAnItem_AnInvalidOperationExceptionIsThrown()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);
            var itemToRename = await itemProvider.FindItemByNameAsync("Profile1");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await itemProvider.SetUnevaluatedIncludesAsync(new KeyValuePair<IProjectItem, string>[]
                {
                    new(itemToRename!, "New Profile Name")
                });
            });
        }

        [Fact]
        public async Task WhenRemovingAnItem_TheRemoveIsIgnoredIfTheItemTypeIsWrong()
        {
            string? removedProfileName = null;

            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() },
                removeProfileCallback: name => removedProfileName = name);

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);
            
            await itemProvider.RemoveAsync(itemType: "RandomItemType", include: "Profile1");

            Assert.Null(removedProfileName);
        }

        [Fact]
        public async Task WhenRemovingAnItem_TheProfileIsRemovedFromTheLaunchSettings()
        {
            string? removedProfileName = null;

            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() },
                removeProfileCallback: name => removedProfileName = name);

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            await itemProvider.RemoveAsync(itemType: LaunchProfileProjectItemProvider.ItemType, include: "Profile1");

            Assert.Equal(expected: "Profile1", actual: removedProfileName);
        }

        [Fact]
        public async Task WhenRetrievingAProjectItem_TheEvaluatedIncludeIsTheProfileName()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            Assert.NotNull(projectItem);

            Assert.Equal(expected: "Profile2", actual: projectItem.EvaluatedInclude);
        }

        [Fact]
        public async Task WhenRetrievingAProjectItem_TheUnevaluatedIncludeIsTheProfileName()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            Assert.NotNull(projectItem);

            Assert.Equal(expected: "Profile2", actual: projectItem.UnevaluatedInclude);
        }

        [Fact]
        public async Task WhenRetrievingAProjectItem_TheItemTypeIsLaunchProfile()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            Assert.NotNull(projectItem);

            Assert.Equal(expected: LaunchProfileProjectItemProvider.ItemType, actual: projectItem.ItemType);
        }

        [Fact]
        public async Task WhenRetrievingAProjectItem_TheIncludesAsPathsAreTheEmptyString()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            Assert.NotNull(projectItem);

            Assert.Equal(expected: string.Empty, actual: projectItem.EvaluatedIncludeAsFullPath);
            Assert.Equal(expected: string.Empty, actual: projectItem.EvaluatedIncludeAsRelativePath);
        }

        [Fact]
        public async Task WhenSettingTheUnevaluatedIncludeOnAProjectItem_AnInvalidOperationExceptionIsThrown()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await projectItem!.SetUnevaluatedIncludeAsync("New Profile Name");
            });
        }

        [Fact]
        public async Task WhenSettingTheItemTypeOnAProjectItem_AnInvalidOperationExceptionIsThrown()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await projectItem!.SetItemTypeAsync("RandomItemType");
            });
        }

        [Fact]
        public async Task WhenRetrievingAProjectItem_ThePropertiesContextHasTheExpectedValues()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.ImplementFullPath(@"C:\alpha\beta\gamma.csproj"),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");
            var propertiesContext = projectItem!.PropertiesContext!;

            Assert.Equal(expected: @"C:\alpha\beta\gamma.csproj", actual: propertiesContext.File);
            Assert.True(propertiesContext.IsProjectFile);
            Assert.Equal(expected: "Profile2", actual: propertiesContext.ItemName);
            Assert.Equal(expected: LaunchProfileProjectItemProvider.ItemType, actual: propertiesContext.ItemType);
        }

        [Fact]
        public async Task WhenTellingAProjectItemToRemoveItself_TheLaunchProfileIsRemoved()
        {
            string? removedProfileName = null;

            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() },
                removeProfileCallback: name => removedProfileName = name);

            var itemProvider = new LaunchProfileProjectItemProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider);

            var projectItem = await itemProvider.FindItemByNameAsync("Profile2");

            await projectItem!.RemoveAsync();

            Assert.Equal(expected: "Profile2", actual: removedProfileName);
        }

        private class TestProjectPropertiesContext : IProjectPropertiesContext
        {
            public TestProjectPropertiesContext(bool isProjectFile, string file, string? itemType, string? itemName)
            {
                IsProjectFile = isProjectFile;
                File = file;
                ItemType = itemType;
                ItemName = itemName;
            }

            public bool IsProjectFile { get; }
            public string File { get; }
            public string? ItemType { get; }
            public string? ItemName { get; }
        }
    }
}
