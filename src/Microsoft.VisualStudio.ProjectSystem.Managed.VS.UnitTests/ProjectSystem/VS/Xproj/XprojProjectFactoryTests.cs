// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    public sealed class XprojProjectFactoryTests
    {
        [Theory]
        [InlineData("foo\\bar.xproj",  __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED, true)]
        [InlineData("foo\\bar.csproj", __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_NOREPAIR,   false)]
        public void UpgradeCheck(string projectPath, __VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS expectedFlags, bool shouldLogError)
        {
            var factory = new XprojProjectFactory();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            var result = factory.UpgradeProject_CheckOnly(
                fileName: projectPath,
                logger,
                out int upgradeRequired,
                out Guid migratedProjectFactor,
                out uint upgradeProjectCapabilityFlags);

            Assert.Equal((int)HResult.OK, result);
            Assert.Equal((int)expectedFlags, upgradeRequired);
            Assert.Equal(typeof(XprojProjectFactory).GUID, migratedProjectFactor);
            Assert.Equal(default, upgradeProjectCapabilityFlags);

            if (shouldLogError)
            {
                LogMessage message = Assert.Single(loggedMessages);
                Assert.Equal(projectPath, message.File);
                Assert.Equal(Path.GetFileNameWithoutExtension(projectPath), message.Project);
                Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, message.Level);
                Assert.Equal(VSResources.XprojNotSupported, message.Message);
            }
            else
            {
                Assert.Empty(loggedMessages);
            }
        }

        [Fact]
        public void GetSccInfo_Throws()
        {
            var factory = new XprojProjectFactory();

            Assert.Throws<NotImplementedException>(() => factory.GetSccInfo("file.xproj", out _, out _, out _, out _));
        }

        [Fact]
        public void UpgradeProject_DoesNothing()
        {
            var factory = new XprojProjectFactory();

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
