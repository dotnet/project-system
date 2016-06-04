// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [ProjectSystemTrait]
    public class ManagedDebuggerImageTypeServiceTests
    {
        [Fact]
        public void Constructor_NullAsProperties_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("properties", () => {
                new ManagedDebuggerImageTypeService((ProjectProperties)null);
            });
        }

        [Fact]
        public void AppUserModelID_ThrowsNotSupported()
        {
            var service = CreateInstance();

            Assert.Throws<NotSupportedException>(() => {

                var ignored = service.AppUserModelID;
            });
        }

        [Fact]
        public void TargetImageClrType_ReturnsManaged()
        {
            var service = CreateInstance();

            var result = service.TargetImageClrType;

            Assert.Equal(ImageClrType.Managed, result);
        }

        [Fact]
        public void PackageMoniker_ThrowsNotSupported()
        {
            var service = CreateInstance();

            Assert.Throws<NotSupportedException>(() => {

                var ignored = service.PackageMoniker;
            });
        }

        [Fact]
        public void GetIs64BitAsync_ThrowsNotSupported()
        {
            var service = CreateInstance();

            Assert.Throws<NotSupportedException>(() => {
                service.GetIs64BitAsync();
            });
        }

        [Theory]
        [InlineData("AppContainerExe")]
        [InlineData("Library")]
        [InlineData("winexe")]
        [InlineData("WinMDObj")]
        [InlineData("Foo")]
        public async Task GetIsConsoleAppAsync_WhenOutputTypeNotExe_ReturnsFalse(string outputType)
        {
            var service = CreateInstance(outputType);

            var result = await service.GetIsConsoleAppAsync();

            Assert.False(result);
        }

        [Theory]
        [InlineData("exe")]
        [InlineData("EXE")]
        [InlineData("Exe")]
        public async Task GetIsConsoleAppAsync_WhenOutputTypeExe_ReturnsTrue(string outputType)
        {
            var service = CreateInstance(outputType);

            var result = await service.GetIsConsoleAppAsync();

            Assert.True(result);
        }

        private ManagedDebuggerImageTypeService CreateInstance()
        {
            var project = IUnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project);

            return new ManagedDebuggerImageTypeService(properties);
        }

        private ManagedDebuggerImageTypeService CreateInstance(string outputType)
        {
            var data = new PropertyPageData() {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.OutputTypeProperty,
                Value = outputType
            };

            var project = IUnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project, data);

            return new ManagedDebuggerImageTypeService(properties);
        }
    }
}
