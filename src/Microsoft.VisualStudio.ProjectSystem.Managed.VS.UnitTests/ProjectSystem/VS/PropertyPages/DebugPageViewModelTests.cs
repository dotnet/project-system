// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    public class DebugPageViewModelTests
    {
        [ProjectSystemTrait]
        private class ViewModelData
        {
            public TestUnconfiguredPropertyProvider UnconfiguredProvider { get; set; }
            public IList<ILaunchProfile> Profiles { get; set; }
            public IIISSettings IISSettings { get; set; }
            public ILaunchSettingsProvider ProfileProvider { get; set;}
            public ILaunchSettings LaunchProfiles { get; set; }
        }
        
        private Mock<DebugPageViewModel> CreateViewModel(ViewModelData data)
        {
            // Setup the debug profiles
            var mockSourceBlock = new Mock<IReceivableSourceBlock<ILaunchSettings>>();
            var mockProfiles = new Mock<ILaunchSettings>();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");
            var viewModel = new Mock<DebugPageViewModel>(false, unconfiguredProject);

            mockSourceBlock.Setup<IDisposable>(m => m.LinkTo(It.IsAny<ITargetBlock<ILaunchSettings>>(), It.IsAny<DataflowLinkOptions>())).Callback
                (
                    (ITargetBlock<ILaunchSettings> targetBlock, DataflowLinkOptions options) =>
                    {
                        targetBlock.Post(mockProfiles.Object);
                        targetBlock.Complete();
                    }
                ).Returns(() => null);

            mockProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return data.Profiles == null ?  null : data.Profiles.ToImmutableList();
            });

            mockProfiles.Setup(m => m.IISSettings).Returns(() =>
            {
                return data.IISSettings == null ?  null : data.IISSettings;
            });

            mockProfiles.Setup(m => m.ProfilesAreDifferent(It.IsAny<IList<ILaunchProfile>>())).Returns((IList<ILaunchProfile> profilesToCompare) =>
            {
                bool detectedChanges = data.Profiles == null || data.Profiles.Count != profilesToCompare.Count;
                if (!detectedChanges)
                {
                    // Now compare each item
                    foreach (var profile in profilesToCompare)
                    {
                        var existingProfile = data.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profile.Name));
                        if (existingProfile == null || !LaunchProfile.ProfilesAreEqual(profile, existingProfile, true))
                        {
                            detectedChanges = true;
                            break;
                        }
                    }
                }
                return detectedChanges;
            });

            mockProfiles.Setup(m => m.IISSettingsAreDifferent(It.IsAny<IISSettingsProfile>())).Returns<IISSettingsProfile>((settingsToCompare) =>
            {
                if(data.IISSettings == null)
                {
                    // Treat empty and null as equivalent
                    return !(settingsToCompare == null || IISSettingsProfile.IsEmptySettings(settingsToCompare));
                }
                else if(settingsToCompare == null)
                {
                    return !IISSettingsProfile.IsEmptySettings(data.IISSettings);
                }
        
                // Compare each item
                return IISSettingsProfile.SettingsDiffer(data.IISSettings, settingsToCompare);
            });

            data.LaunchProfiles = mockProfiles.Object;

            var mockProfileProvider = new Mock<ILaunchSettingsProvider>();
            mockProfileProvider.SetupGet(m => m.SourceBlock).Returns(mockSourceBlock.Object);
            mockProfileProvider.SetupGet(m => m.CurrentSnapshot).Returns(data.LaunchProfiles);
            mockProfileProvider.Setup(m => m.UpdateAndSaveSettingsAsync(It.IsAny<ILaunchSettings>())).Callback((ILaunchSettings newProfiles) =>
                    {
                        data.Profiles = new List<ILaunchProfile>(newProfiles.Profiles);
                        data.IISSettings = newProfiles.IISSettings;
                    }
                ).Returns(Task.Run(() => { })).Verifiable();

            data.ProfileProvider = mockProfileProvider.Object;

            viewModel.CallBase = true;
            viewModel.Protected().Setup<ILaunchSettingsProvider>("GetDebugProfileProvider").Returns(mockProfileProvider.Object);
            return viewModel;
        }

        [Fact]
        public async Task DebugPageViewModel_AddAndDeleteProfiles()
        {
            TestUnconfiguredPropertyProvider unconfiguredProvider = new TestUnconfiguredPropertyProvider();
       
            var profiles = new List<ILaunchProfile>();
            profiles.Add(new LaunchProfile() { Name = "MyCommand" });

            var viewModelData = new ViewModelData()
            {
                UnconfiguredProvider = unconfiguredProvider,
                Profiles = profiles,
            };

            var viewModel = CreateViewModel(viewModelData);
            await viewModel.Object.Initialize();
            Utilities.WaitForAsyncOperation(1800000, () => viewModel.Object.EnvironmentVariables != null);

            viewModel.Object.DebugProfiles.Add(new LaunchProfile() { Name = "NewProfile", Kind = ProfileKind.CustomizedCommand });

            viewModel.Object.SelectedDebugProfile = viewModel.Object.DebugProfiles[1];
            Assert.False(viewModel.Object.IsCustomType);

            // swap out for new set of profiles
            viewModelData.Profiles = new List<ILaunchProfile>();
            viewModelData.Profiles.Add(new LaunchProfile() { Name = "MyOtherCommand" });

            viewModel.Object.InitializeDebugTargetsCore(viewModelData.LaunchProfiles);
            Assert.Equal(1, viewModel.Object.DebugProfiles.Count);

            Assert.Equal(viewModelData.Profiles[0].Name, viewModelData.Profiles[0].Name);
            viewModel.Object.SelectedCommandName = "NewCommandName";
            await viewModel.Object.Save();
            Assert.Equal("NewCommandName", viewModel.Object.SelectedCommandName);

            viewModel.Object.DeleteProfileCommand.Execute(null);
            Assert.Equal(0, viewModel.Object.DebugProfiles.Count);
            Assert.Equal(viewModel.Object.SelectedDebugProfile, null);
        }
        
        [Fact]
        public void DebugPageViewModel_UICommands()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");
            var viewModel = new DebugPageViewModel(false,unconfiguredProject);
 
            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.BrowseDirectoryCommand);
            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.BrowseExecutableCommand);
            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.NewProfileCommand);
            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.DeleteProfileCommand);
        }

        [Fact]
        public async Task DebugPageViewModel_EnvironmentCommands()
        {
            TestUnconfiguredPropertyProvider unconfiguredProvider = new TestUnconfiguredPropertyProvider();
        
            var profiles = new List<ILaunchProfile>();
            profiles.Add(new LaunchProfile() { Name = "Run", WorkingDirectory = "c:\\somepath", Kind = ProfileKind.BuiltInCommand });
            profiles.Add(new LaunchProfile() { Name = "MyCommand", Kind = ProfileKind.Executable });

            var viewModelData = new ViewModelData()
            {
                UnconfiguredProvider = unconfiguredProvider,
                Profiles = profiles,
            };

            // setup the viewmodel
            var viewModel = CreateViewModel(viewModelData);

            // verify initialize
            await viewModel.Object.Initialize();
            Utilities.WaitForAsyncOperation(3000, () => viewModel.Object.EnvironmentVariables != null);

            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.Object.AddEnvironmentVariableRowCommand);
            viewModel.Object.FocusEnvironmentVariablesGridRow += new EventHandler((object s, EventArgs e) =>
            {
                Assert.Equal(viewModel.Object, s);
            });

            Assert.False(viewModel.Object.RemoveEnvironmentVariablesRow);
            Assert.True(viewModel.Object.EnvironmentVariablesValid);
            viewModel.Object.AddEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(1, viewModel.Object.EnvironmentVariables.Count);
            var addedVariable = viewModel.Object.EnvironmentVariables[0];
            Assert.Equal("Key", addedVariable.Name);
            Assert.Equal("Value", addedVariable.Value);
            var validationDummy = addedVariable["Name"];
            Assert.Equal(0, viewModel.Object.EnvironmentVariablesRowSelectedIndex);
            Assert.True(viewModel.Object.RemoveEnvironmentVariablesRow);
            Assert.True(viewModel.Object.EnvironmentVariablesValid);

            viewModel.Object.AddEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(2, viewModel.Object.EnvironmentVariables.Count);
            addedVariable = viewModel.Object.EnvironmentVariables[1];
            Assert.Equal("Key", addedVariable.Name);
            Assert.Equal("Value", addedVariable.Value);
            validationDummy = addedVariable["Name"];
            Assert.False(viewModel.Object.EnvironmentVariablesValid);
            addedVariable.Name = "NewKey2";
            validationDummy = addedVariable["Name"];
            Assert.True(viewModel.Object.EnvironmentVariablesValid);
            Assert.Equal(1, viewModel.Object.EnvironmentVariablesRowSelectedIndex);

            viewModel.Object.AddEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(3, viewModel.Object.EnvironmentVariables.Count);
            addedVariable = viewModel.Object.EnvironmentVariables[2];
            Assert.Equal("Key", addedVariable.Name);
            Assert.Equal("Value", addedVariable.Value);
            addedVariable.Name = "NewKey3";
            Assert.Equal(2, viewModel.Object.EnvironmentVariablesRowSelectedIndex);

            viewModel.Object.AddEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(4, viewModel.Object.EnvironmentVariables.Count);
            addedVariable = viewModel.Object.EnvironmentVariables[3];
            Assert.Equal("Key", addedVariable.Name);
            Assert.Equal("Value", addedVariable.Value);
            addedVariable.Name = "NewKey4";
            Assert.Equal(3, viewModel.Object.EnvironmentVariablesRowSelectedIndex);

            Assert.IsType<VS.Utilities.DelegateCommand>(viewModel.Object.RemoveEnvironmentVariableRowCommand);

            viewModel.Object.RemoveEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(3, viewModel.Object.EnvironmentVariables.Count);
            Assert.Equal(viewModel.Object.EnvironmentVariables.Count - 1, viewModel.Object.EnvironmentVariablesRowSelectedIndex);

            viewModel.Object.EnvironmentVariablesRowSelectedIndex = 0;
            viewModel.Object.RemoveEnvironmentVariableRowCommand.Execute(null);
            Assert.Equal(2, viewModel.Object.EnvironmentVariables.Count);
            Assert.Equal(0, viewModel.Object.EnvironmentVariablesRowSelectedIndex);
        }
       [Fact]
       public async Task DebugPageViewModel_Helpers()
       {
           TestUnconfiguredPropertyProvider unconfiguredProvider = new TestUnconfiguredPropertyProvider();
           var profiles = new List<ILaunchProfile>();
           profiles.Add(new LaunchProfile() { Name = "MyCommand" });

           var viewModelData = new ViewModelData()
           {
               UnconfiguredProvider = unconfiguredProvider,
               Profiles = profiles,
          };

           var viewModel = CreateViewModel(viewModelData);
           await viewModel.Object.Initialize();
           Utilities.WaitForAsyncOperation(3000, () => viewModel.Object.EnvironmentVariables != null);

           Assert.False(viewModel.Object.IsNewProfileNameValid("MyCommand"));
           Assert.True(viewModel.Object.IsNewProfileNameValid("MyOtherCommand"));
           Assert.Equal("newProfile1", viewModel.Object.GetNewProfileName());
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
           Utilities.WaitForAsyncOperation(3000, () => viewModel.Object.EnvironmentVariables != null);
           Assert.False(viewModel.Object.HasProfiles);
           Assert.False(viewModel.Object.IsProfileSelected);
           Assert.False(viewModel.Object.IsBuiltInProfile);
           Assert.False(viewModel.Object.IsCustomType);
           Assert.False(viewModel.Object.IsIISExpress);
           Assert.False(viewModel.Object.IsCommand);
           Assert.False(viewModel.Object.IsExecutable);
           Assert.False(viewModel.Object.HasLaunchOption);
           Assert.Equal(string.Empty, viewModel.Object.WorkingDirectory);
           Assert.Equal(string.Empty, viewModel.Object.LaunchPage);
           Assert.Equal(string.Empty, viewModel.Object.ExecutablePath);
           Assert.Equal(string.Empty, viewModel.Object.CommandLineArguments);
           Assert.Equal(string.Empty, viewModel.Object.SelectedCommandName);
       }
        
    }
}
