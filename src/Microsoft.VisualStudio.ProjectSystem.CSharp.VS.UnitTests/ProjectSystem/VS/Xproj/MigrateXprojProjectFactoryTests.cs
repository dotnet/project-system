// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [ProjectSystemTrait]
    public class MigrateXprojProjectFactoryTests
    {
        [Fact]
        public void MigrateXprojProjectFactory_NullProcessRunner_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("runner", () => new MigrateXprojProjectFactory(null));
        }

        [Fact]
        public void MigrateXprojProjectFactory_NonExistantProjectJson_DoesNotBackUp()
        {
            var backupDirectory = @"C:\NonExistent";
            var xproj = @"C:\NonExistent\XprojMigrationTests.xproj";
            var projectJson = @"C:\NonExistent\project.json";

            var procRunner = ProcessRunnerFactory.CreateRunner();
            var migrator = new MigrateXprojProjectFactory(procRunner);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.False(migrator.BackupProject(backupDirectory, xproj, "XprojMigrationTests", logger));

            Assert.Equal(1, loggedMessages.Count);
            Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, loggedMessages[0].Level);
            Assert.Equal("XprojMigrationTests", loggedMessages[0].Project);
            Assert.Equal(projectJson, loggedMessages[0].File);
            Assert.Equal($"Failed to migrate XProj project XprojMigrationTests. Could not find project.json at {projectJson}.", loggedMessages[0].Message);
        }

        [Fact]
        public void MigrateXprojProjectFactory_ValidPaths_CallMigrateCorrectly()
        {
            var projectDirectory = @"C:\Test";
            var xproj = @"C:\Test\XprojMigrationTests.xproj";

            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(pr =>
            {
                Assert.Equal("dotnet.exe", pr.FileName);
                Assert.Equal("migrate -s -p \"C:\\Test\" -x \"C:\\Test\\XprojMigrationTests.xproj\"", pr.Arguments);
            });
            var migrator = new MigrateXprojProjectFactory(procRunner);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.True(migrator.MigrateProject(projectDirectory, xproj, "XprojMigrationTests", logger));
            Assert.Equal(0, loggedMessages.Count);
        }

        private Tuple<string, string, string> CreateTempProjectLocation()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"XprojMigrationTests_{Guid.NewGuid().ToString()}");
            Directory.CreateDirectory(directory);
            var backupDirectory = Path.Combine(directory, "backup");
            Directory.CreateDirectory(backupDirectory);
            var xproj = Path.Combine(directory, "XprojMigrationTests.xproj");
            File.Create(xproj);
            var projectJson = Path.Combine(directory, "project.json");
            File.Create(projectJson);

            return Tuple.Create(backupDirectory, xproj, projectJson);
        }
    }
}
