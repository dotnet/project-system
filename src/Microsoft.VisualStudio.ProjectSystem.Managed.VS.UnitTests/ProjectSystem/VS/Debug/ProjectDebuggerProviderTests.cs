// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [ProjectSystemTrait]
    public class ProjectDebuggerProviderTests
    {
        Mock<IDebugProfileLaunchTargetsProvider> _mockWebProvider =  new Mock<IDebugProfileLaunchTargetsProvider>();
        Mock<IDebugProfileLaunchTargetsProvider> _mockDockerProvider =  new Mock<IDebugProfileLaunchTargetsProvider>();
        Mock<IDebugProfileLaunchTargetsProvider> _mockExeProvider =  new Mock<IDebugProfileLaunchTargetsProvider>();
        OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> _launchProviders = 
            new OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        Mock<ConfiguredProject> _configuredProjectMoq = new Mock<ConfiguredProject>();
        Mock<ILaunchSettingsProvider> _LaunchSettingsProviderMoq = new Mock<ILaunchSettingsProvider>();

        List<IDebugLaunchSettings> _webProviderSettings = new List<IDebugLaunchSettings>();
        List<IDebugLaunchSettings> _dockerProviderSettings = new List<IDebugLaunchSettings>();
        List<IDebugLaunchSettings> _exeProviderSettings = new List<IDebugLaunchSettings>();

        // Set this to have ILaunchSettingsProvider return this profile (null by default)
        ILaunchProfile _activeProfile;

        public ProjectDebuggerProviderTests()
        {
            _mockWebProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "IISExpress");
            _mockWebProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => {return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_webProviderSettings);});
            _mockDockerProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "Docker");
            _mockDockerProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => {return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_dockerProviderSettings);});
            _mockExeProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => string.IsNullOrEmpty(p.CommandName) || p.CommandName == "Project");
            _mockExeProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => {return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_exeProviderSettings);});

            Mock<IOrderPrecedenceMetadataView> mockMetadata  = new Mock<IOrderPrecedenceMetadataView>();
           _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockWebProvider.Object, mockMetadata.Object));
            _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockDockerProvider.Object, mockMetadata.Object));
            _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockExeProvider.Object, mockMetadata.Object));

            _LaunchSettingsProviderMoq.Setup(x => x.ActiveProfile).Returns(() => _activeProfile);
            _LaunchSettingsProviderMoq.Setup(x => x.WaitForFirstSnapshotAsync(It.IsAny<int>())).Returns(() =>
            {
                if (_activeProfile != null)
                {
                    return Task.FromResult((ILaunchSettings)new LaunchSettings(new List<ILaunchProfile>() { _activeProfile }, null, _activeProfile.Name));
                }

                return Task.FromResult((ILaunchSettings)new LaunchSettings());
            });
        }
        
        
        [Fact]
        public void ProjectDebuggerProvider_GetDebugEngineForFrameworkTests()
        {

            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectDebuggerProvider.GetManagedDebugEngineForFramework(".NetStandardApp"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectDebuggerProvider.GetManagedDebugEngineForFramework(".NetStandard"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectDebuggerProvider.GetManagedDebugEngineForFramework(".NetCore"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectDebuggerProvider.GetManagedDebugEngineForFramework(".NetCoreApp"));
            Assert.Equal(DebuggerEngines.ManagedOnlyEngine, ProjectDebuggerProvider.GetManagedDebugEngineForFramework(".NETFramework"));
        }        

        [Fact]
        public async Task ProjectDebuggerProvider_CanLaunchAsyncTests()
        {
            Mock<ConfiguredProject> configuredProjectMoq = new Mock<ConfiguredProject>();
            var debugger = new ProjectDebuggerProvider(configuredProjectMoq.Object, new Mock<ILaunchSettingsProvider>().Object);

            bool result = await debugger.CanLaunchAsync(DebugLaunchOptions.NoDebug);
            Assert.True(result);
            result = await debugger.CanLaunchAsync(0);
            Assert.True(result);
        }
           
        [Fact]
        public void ProjectDebuggerProvider_GetLaunchTargetsProviderForProfileTests()
        {
            var debugger = new ProjectDebuggerProvider(_configuredProjectMoq.Object, _LaunchSettingsProviderMoq.Object, _launchProviders);
            Assert.Equal(_mockWebProvider.Object, debugger.GetLaunchTargetsProvider(new LaunchProfile() {Name = "test", CommandName = "IISExpress"}));
            Assert.Equal(_mockDockerProvider.Object, debugger.GetLaunchTargetsProvider(new LaunchProfile() {Name = "test", CommandName = "Docker"}));
            Assert.Equal(_mockExeProvider.Object, debugger.GetLaunchTargetsProvider(new LaunchProfile() {Name = "test", CommandName = "Project"}));
            Assert.Equal(null, debugger.GetLaunchTargetsProvider(new LaunchProfile() {Name = "test",CommandName = "IIS"}));
        }

        [Fact]
        public async Task ProjectDebuggerProvider_QueryDebugTargetsAsyncCorrectProvider()
        {
            var debugger = new ProjectDebuggerProvider(_configuredProjectMoq.Object, _LaunchSettingsProviderMoq.Object, _launchProviders);

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "IISExpress" };
            var result = await debugger.QueryDebugTargetsAsync(0);
            Assert.Equal(_webProviderSettings, result);

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "Docker" };
            result = await debugger.QueryDebugTargetsAsync(0);
            Assert.Equal(_dockerProviderSettings, result);

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "Project" };
            result = await debugger.QueryDebugTargetsAsync(0);
            Assert.Equal(_exeProviderSettings, result);
        }

        [Fact]
        public async Task ProjectDebuggerProvider_QueryDebugTargetsNoLaunchProfiler()
        {
            var debugger = new ProjectDebuggerProvider(_configuredProjectMoq.Object, _LaunchSettingsProviderMoq.Object, _launchProviders);
            _activeProfile = null;
            try
            {
                var result = await debugger.QueryDebugTargetsAsync(0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(VSResources.ActiveLaunchProfileNotFound, ex.Message);
            }
        }

        [Fact]
        public async Task ProjectDebuggerProvider_QueryDebugTargetsNoInstalledProvider()
        {
            var debugger = new ProjectDebuggerProvider(_configuredProjectMoq.Object, _LaunchSettingsProviderMoq.Object, _launchProviders);
            _activeProfile = new LaunchProfile() {Name="NoActionProfile", CommandName = "SomeOtherExtension"};
            try
            {
                var result = await debugger.QueryDebugTargetsAsync(0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(string.Format(VSResources.DontKnowHowToRunProfile, _activeProfile.Name), ex.Message);
            }
        }        
    }
}
