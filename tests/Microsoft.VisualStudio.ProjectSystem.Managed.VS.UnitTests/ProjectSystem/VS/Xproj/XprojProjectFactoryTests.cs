// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    public sealed class XprojProjectFactoryTests
    {
        [Fact]
        public void UpgradeCheck()
        {
            string projectPath = "foo\\bar.xproj";

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            var factory = new XprojProjectFactory(new Threading.JoinableTaskContext());
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            factory.UpgradeProject_CheckOnly(
                fileName: projectPath,
                logger,
                out uint upgradeRequired,
                out Guid migratedProjectFactor,
                out uint upgradeProjectCapabilityFlags);

            Assert.Equal((uint)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_DEPRECATED, upgradeRequired);
            Assert.Equal(typeof(XprojProjectFactory).GUID, migratedProjectFactor);
            Assert.Equal(default, upgradeProjectCapabilityFlags);

            LogMessage message = Assert.Single(loggedMessages);
            Assert.Equal(projectPath, message.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(projectPath), message.Project);
            Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, message.Level);
            Assert.Equal(VSResources.XprojNotSupported, message.Message);
        }
    }
}
