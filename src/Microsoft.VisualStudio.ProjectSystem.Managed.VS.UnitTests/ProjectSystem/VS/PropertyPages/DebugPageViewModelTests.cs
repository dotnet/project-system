// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [ProjectSystemTrait]
    public class DebugPageViewModelTests
    {
        private class ViewModelData
        {
            public TestUnconfiguredPropertyProvider UnconfiguredProvider { get; set; }
            public IList<ILaunchProfile> Profiles { get; set; }
            public ILaunchSettingsProvider ProfileProvider { get; set; }
            public ILaunchSettings LaunchProfiles { get; set; }
            public IList<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>> UIProviders { get; set; } = new List<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>>();
            public TaskCompletionSource<bool> FirstSnapshotComplete { get; set; }
        }

        private Mock<DebugPageViewModel> CreateViewModel(ViewModelData data)
        {
            // Setup the debug profiles
            var mockSourceBlock = new Mock<IReceivableSourceBlock<ILaunchSettings>>();
            var mockProfiles = new Mock<ILaunchSettings>();
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");

            data.FirstSnapshotComplete = new TaskCompletionSource<bool>();
            var viewModel = new Mock<DebugPageViewModel>(data.FirstSnapshotComplete, unconfiguredProject);

            mockSourceBlock.Setup(m => m.LinkTo(It.IsAny<ITargetBlock<ILaunchSettings>>(), It.IsAny<DataflowLinkOptions>())).Callback
                (
                    (ITargetBlock<ILaunchSettings> targetBlock, DataflowLinkOptions options) =>
                    {
                        targetBlock.Post(mockProfiles.Object);
                        targetBlock.Complete();
                    }
                ).Returns(() => null);

            mockProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return data.Profiles?.ToImmutableList();
            });
            
            data.LaunchProfiles = mockProfiles.Object;

            var mockProfileProvider = new Mock<ILaunchSettingsProvider>();
            mockProfileProvider.SetupGet(m => m.SourceBlock).Returns(mockSourceBlock.Object);
            mockProfileProvider.SetupGet(m => m.CurrentSnapshot).Returns(data.LaunchProfiles);
            mockProfileProvider.Setup(m => m.UpdateAndSaveSettingsAsync(It.IsAny<ILaunchSettings>())).Callback((ILaunchSettings newProfiles) =>
                    {
                        data.Profiles = new List<ILaunchProfile>(newProfiles.Profiles);
                    }
                ).Returns(Task.Run(() => { })).Verifiable();

            data.ProfileProvider = mockProfileProvider.Object;

            viewModel.CallBase = true;
            viewModel.Protected().Setup<ILaunchSettingsProvider>("GetDebugProfileProvider").Returns(mockProfileProvider.Object);
            viewModel.Protected().Setup<IEnumerable<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>>>("GetUIProviders").Returns(data.UIProviders);
            return viewModel;
        }

        [Fact]
        public void DebugPageViewModel_UICommands()
        {
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");
            var viewModel = new DebugPageViewModel(null, unconfiguredProject);

            Assert.IsType<Utilities.DelegateCommand>(viewModel.BrowseDirectoryCommand);
            Assert.IsType<Utilities.DelegateCommand>(viewModel.BrowseExecutableCommand);
            Assert.IsType<Utilities.DelegateCommand>(viewModel.NewProfileCommand);
            Assert.IsType<Utilities.DelegateCommand>(viewModel.DeleteProfileCommand);
        }
        
        [Fact]
        public async Task DebugPageViewModel_NoProfiles()
        {
            TestUnconfiguredPropertyProvider unconfiguredProvider = new TestUnconfiguredPropertyProvider();
            var profiles = new List<ILaunchProfile>();

            var viewModelData = new ViewModelData()
            {
                UnconfiguredProvider = unconfiguredProvider,
                Profiles = profiles,
            };

            var viewModel = CreateViewModel(viewModelData);
            await viewModel.Object.Initialize();
            Assert.False(viewModel.Object.HasProfiles);
            Assert.False(viewModel.Object.IsProfileSelected);
            Assert.False(viewModel.Object.SupportsExecutable);
            Assert.False(viewModel.Object.HasLaunchOption);
            Assert.Equal(string.Empty, viewModel.Object.WorkingDirectory);
            Assert.Equal(string.Empty, viewModel.Object.LaunchPage);
            Assert.Equal(string.Empty, viewModel.Object.ExecutablePath);
            Assert.Equal(string.Empty, viewModel.Object.CommandLineArguments);
        }

        [Fact]
        public async Task DebugPageViewModel_PropertyChange()
        {
            TestUnconfiguredPropertyProvider unconfiguredProvider = new TestUnconfiguredPropertyProvider();
            var profiles = new List<ILaunchProfile>()
            {
                {new LaunchProfile() {Name="p1", CommandName="test", DoNotPersist = true}}
            };

            var viewModelData = new ViewModelData()
            {
                UnconfiguredProvider = unconfiguredProvider,
                Profiles = profiles,
                UIProviders = new List<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>>()
                {
                    {new Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>(() => 
                    {
                        var uiProvider = new Mock<ILaunchSettingsUIProvider>();
                        uiProvider.Setup(m => m.CustomUI).Returns((UserControl)null);
                        uiProvider.Setup(m => m.ShouldEnableProperty(It.IsAny<string>())).Returns(true);
                        uiProvider.Setup(m => m.CommandName).Returns("test");
                        return uiProvider.Object;
                    }, new Mock<IOrderPrecedenceMetadataView>().Object)}
                }
            };

            var viewModel = CreateViewModel(viewModelData);
            await viewModel.Object.Initialize();
            await viewModelData.FirstSnapshotComplete.Task;

            Assert.True(viewModel.Object.HasProfiles);
            Assert.True(viewModel.Object.IsProfileSelected);
            Assert.True(viewModel.Object.SelectedDebugProfile.IsInMemoryObject());

            // Change a property, should trigger the selected profile to no longer be in-memory
            viewModel.Object.CommandLineArguments = "-arg";
            Assert.False(viewModel.Object.SelectedDebugProfile.IsInMemoryObject());
        }
    }
}
