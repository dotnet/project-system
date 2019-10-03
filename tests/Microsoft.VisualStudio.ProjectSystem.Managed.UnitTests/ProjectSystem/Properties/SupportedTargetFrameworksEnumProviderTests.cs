// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class SupportedTargetFrameworksEnumProviderTests
    {
        [Fact]
        public async Task GetProviderAsync_ReturnsNonNullGenerator()
        {
            var projectAccessor = IProjectAccessorFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task GetListedValuesAsync_ReturnsSupportedTargetFrameworksItems()
        {
            string project =
@"<Project>
    <ItemGroup>
        <SupportedTargetFramework Include="".NETCoreApp,Version=v1.0"" DisplayName="".NET Core 1.0"" />
        <SupportedTargetFramework Include="".NETCoreApp,Version=v1.1"" DisplayName="".NET Core 1.1"" />
        <SupportedTargetFramework Include="".NETCoreApp,Version=v2.0"" DisplayName="".NET Core 2.0"" />
    </ItemGroup>
</Project>";

            var projectAccessor = IProjectAccessorFactory.Create(project);

            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);
            var values = await generator.GetListedValuesAsync();

            AssertEx.CollectionLength(values, 3);
            Assert.Equal(new List<string> { ".NETCoreApp,Version=v1.0", ".NETCoreApp,Version=v1.1", ".NETCoreApp,Version=v2.0" }, values.Select(v => v.Name));
            Assert.Equal(new List<string> { ".NET Core 1.0", ".NET Core 1.1", ".NET Core 2.0" }, values.Select(v => v.DisplayName));
        }

        [Fact]
        public async Task TryCreateEnumValueAsync_ThrowsNotImplemented()
        {
            var projectAccessor = IProjectAccessorFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create();

            var provider = new SupportedTargetFrameworksEnumProvider(projectAccessor, configuredProject);
            var generator = await provider.GetProviderAsync(null);

            Assert.Throws<NotImplementedException>(() =>
            {
                generator.TryCreateEnumValueAsync("foo");
            });
        }
    }
}
