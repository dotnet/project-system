// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ProjectSystemTrait]
    public class SupportedTargetFrameworksEnumProviderTests
    {
        [Fact]
        public void Constructor_NullProjectXmlAccessor_ThrowsArgumentNullException()
        {
            var configuredProject = ConfiguredProjectFactory.Create();
            Assert.Throws<ArgumentNullException>("projectXmlAccessor", () =>
            {
                new SupportedTargetFrameworksEnumProvider(null, configuredProject);
            });
        }

        [Fact]
        public void Constructor_NullConfiguredProject_ThrowsArgumentNullException()
        {
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create();

            Assert.Throws<ArgumentNullException>("configuredProject", () =>
            {
                new SupportedTargetFrameworksEnumProvider(projectXmlAccessor, null);
            });
        }

        [Fact]
        public async Task Constructor()
        {
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectXmlAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task GetListedValues()
        {
            var projectXmlAccessor = IProjectXmlAccessorFactory.WithItems("SupportedTargetFramework", "DisplayName", new[] {
                (name: ".NETCoreApp,Version=v1.0", metadataValue: ".NET Core 1.0"),
                (name: ".NETCoreApp,Version=v1.1", metadataValue: ".NET Core 1.1"),
                (name: ".NETCoreApp,Version=v2.0", metadataValue: ".NET Core 2.0"),
            });
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectXmlAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);
            var values = await generator.GetListedValuesAsync();

            AssertEx.CollectionLength(values, 3);
            Assert.Equal(new List<string> { ".NETCoreApp,Version=v1.0", ".NETCoreApp,Version=v1.1", ".NETCoreApp,Version=v2.0" }, values.Select(v => v.Name));
            Assert.Equal(new List<string> { ".NET Core 1.0", ".NET Core 1.1", ".NET Core 2.0" }, values.Select(v => v.DisplayName));
        }

        [Fact]
        public async Task TryCreateEnumValue()
        {
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectXmlAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.Throws<NotImplementedException>(() =>
            {
                generator.TryCreateEnumValueAsync("foo");
            });
        }
    }
}
