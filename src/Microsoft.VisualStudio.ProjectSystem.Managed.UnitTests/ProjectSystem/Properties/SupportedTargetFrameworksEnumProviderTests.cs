// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ProjectSystemTrait]
    public class SupportedTargetFrameworksEnumProviderTests
    {
        [Fact]
        public void Constructor_NullProjectLockService_ThrowsArgumentNullException()
        {
            var configuredProject = ConfiguredProjectFactory.Create();
            Assert.Throws<ArgumentNullException>("projectLockService", () =>
            {
                new SupportedTargetFrameworksEnumProvider(null, configuredProject);
            });
        }

        [Fact]
        public void Constructor_NullConfiguredProject_ThrowsArgumentNullException()
        {
            var projectLockService = IProjectLockServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("configuredProject", () =>
            {
                new SupportedTargetFrameworksEnumProvider(projectLockService, null);
            });
        }

        [Fact]
        public async Task Constructor()
        {
            var projectLockService = IProjectLockServiceFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectLockService, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task GetListedValues()
        {
            var projectLockService = IProjectLockServiceFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectLockService, configuredProject);
            var generator = await provider.GetProviderAsync(null);
            var values = await generator.GetListedValuesAsync();

            //Assert.Equal(3, values.Count);
            //Assert.Equal(new List<string> { "0", "1", "2" }, values.Select(v => v.DisplayName));
        }

        [Fact]
        public async Task TryCreateEnumValue()
        {
            var projectLockService = IProjectLockServiceFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectLockService, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.Throws<NotImplementedException>(() =>
            {
                generator.TryCreateEnumValueAsync("foo");
            });
        }
    }
}
