// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    public class ReferenceContextProviderTests
    {
        [Fact]
        public void AddFileContextProvider_IsApplicable_True()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapability.ReferenceManagerBrowse });
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new AddFileContextProvider(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void AddFileContextProvider_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new AddFileContextProvider(project);
            Assert.False(context.IsApplicable());
        }

        [Fact]
        public void AssemblyReferencesProviderContext_IsApplicable()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapability.ReferenceManagerAssemblies });
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new AssemblyReferencesProviderContext(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void AssemblyReferencesProviderContext_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new AssemblyReferencesProviderContext(project);
            Assert.False(context.IsApplicable());
        }

        [Fact]
        public void ComReferencesProviderContext_IsApplicable()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapability.ReferenceManagerCOM });
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new ComReferencesProviderContext(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void ComReferencesProviderContext_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new ComReferencesProviderContext(project);
            Assert.False(context.IsApplicable());
        }

        [Fact]
        public void ProjectReferencesProviderContext_IsApplicable()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapability.ReferenceManagerProjects });
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new ProjectReferencesProviderContext(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void ProjectReferencesProviderContext_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new ProjectReferencesProviderContext(project);
            Assert.False(context.IsApplicable());
        }

        [Fact]
        public void SharedProjectReferencesProviderContext_IsApplicable()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapability.ReferenceManagerSharedProjects });
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new SharedProjectReferencesProviderContext(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void SharedProjectReferencesProviderContext_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new SharedProjectReferencesProviderContext(project);
            Assert.False(context.IsApplicable());
        }

        [Fact]
        public void WinRTReferencesProviderContext_IsApplicable()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[]
            {
                ProjectCapabilities.WinRTReferences,
                ProjectCapabilities.SdkReferences,
                ProjectCapability.ReferenceManagerWinRT
            });

            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new WinRTReferencesProviderContext(project);
            Assert.True(context.IsApplicable());
        }

        [Fact]
        public void WinRTReferencesProviderContext_IsApplicable_False()
        {
            var capabilities = IProjectCapabilitiesScopeFactory.Create();
            var project = ConfiguredProjectFactory.Create(capabilities: capabilities);

            var context = new WinRTReferencesProviderContext(project);
            Assert.False(context.IsApplicable());
        }
    }
}
