// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class VsLangProjectPropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullAsVsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("vsProject", () =>
            {
                GetVsLangProjectPropertiesProvider();
            });
        }

        [Fact]
        public void Constructor_NullAsProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectVsServices", () =>
            {
                GetVsLangProjectPropertiesProvider(Mock.Of<VSProject>());
            });
        }

        [Fact]
        public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectProperties", () =>
            {
                GetVsLangProjectPropertiesProvider(Mock.Of<VSProject>(), Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void VsLangProjectPropertiesProvider_VsProject_NotNull()
        {
            var provider = GetVsLangProjectPropertiesProvider(Mock.Of<VSProject>(), Mock.Of<IProjectThreadingService>(), Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(provider);
            Assert.NotNull(provider.VSProject);
        }

        private static VsLangProjectPropertiesProvider GetVsLangProjectPropertiesProvider(
            VSProject vsproject = null, IProjectThreadingService threadingService = null, ActiveConfiguredProject<ProjectProperties> projectProperties = null)
        {
            return new VsLangProjectPropertiesProvider(vsproject, threadingService, projectProperties);
        }
    }
}
