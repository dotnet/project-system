// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    public sealed class MigrateXprojProjectFactoryTests
    {
        [Theory]
        [InlineData("foo\\bar.xproj",  __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED)]
        [InlineData("foo\\bar.csproj", __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR)]
        public void UpgradeCheck(string project, __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS expectedFlags)
        {
            var factory = new MigrateXprojProjectFactory();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            var result = factory.UpgradeProject_CheckOnly(
                fileName: project,
                logger,
                out int upgradeRequired,
                out Guid migratedProjectFactor,
                out uint upgradeProjectCapabilityFlags);

            Assert.Equal((int)HResult.OK, result);
            Assert.Equal((int)expectedFlags, upgradeRequired);
            Assert.Equal(typeof(MigrateXprojProjectFactory).GUID, migratedProjectFactor);
            Assert.Equal(default, upgradeProjectCapabilityFlags);
            Assert.Empty(loggedMessages);
        }

        [Fact]
        public void GetSccInfo_Throws()
        {
            var factory = new MigrateXprojProjectFactory();

            Assert.Throws<NotImplementedException>(() => factory.GetSccInfo("file.xproj", out _, out _, out _, out _));
        }

        [Fact]
        public void UpgradeProject_DoesNothing()
        {
            var factory = new MigrateXprojProjectFactory();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            var result = factory.UpgradeProject(
                "file.xproj",
                0,
                "c:\\temp",
                out var migratedProjectFileLocation,
                logger,
                out var upgradeRequired,
                out var migratedProjectGuid);

            Assert.Equal(VSConstants.S_FALSE, result);
            Assert.Equal(default, migratedProjectFileLocation);
            Assert.Equal(default, upgradeRequired);
            Assert.Equal(default, migratedProjectGuid);
            Assert.Empty(loggedMessages);
        }
    }
}
