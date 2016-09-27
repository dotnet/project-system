// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
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
        public void MigrateXprojProjectFactory_ValidArgs_BackupsCorrectly()
        {
            var tuple = CreateTempProjectLocation();
            var backupDirectory = tuple.Item1;
            var xproj = tuple.Item2;
            var projectJson = tuple.Item3;

            var procRunner = ProcessRunnerFactory.CreateRunner();
            var migrator = new MigrateXprojProjectFactory(procRunner);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.True(migrator.BackupProject(backupDirectory, xproj, "XprojMigrationTests", logger));

            // We expect 2 informational messages about what files were backed up.
            Assert.Equal(2, loggedMessages.Count);
            loggedMessages.ForEach(message =>
            {
                Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, message.Level);
                Assert.Equal("XprojMigrationTests", message.Project);
            });

            // The first message should be about the old xproj, the second about the project.json
            Assert.Equal(xproj, loggedMessages[0].File);
            Assert.Equal(projectJson, loggedMessages[1].File);
            Assert.Equal($"Backing up {xproj} to {Path.Combine(backupDirectory, "XprojMigrationTests.xproj")}.", loggedMessages[0].Message);
            Assert.Equal($"Backing up {projectJson} to {Path.Combine(backupDirectory, "project.json")}.", loggedMessages[1].Message);

            // Finally, assert that there actually are backup files in the backup directory
            var backedUpFiles = Directory.GetFiles(backupDirectory);
            Assert.Equal(2, backedUpFiles.Count());
            Assert.True(backedUpFiles.Any(file => file.EndsWith("project.json")));
            Assert.True(backedUpFiles.Any(file => file.EndsWith("XprojMigrationTests.xproj")));
        }

        [Fact]
        public void MigrateXprojProjectFactory_NonExistantProjectJson_DoesNotBackUp()
        {
            var tuple = CreateTempProjectLocation();
            var backupDirectory = tuple.Item1;
            var xproj = tuple.Item2;
            var projectJson = tuple.Item3;

            var procRunner = ProcessRunnerFactory.CreateRunner();
            var migrator = new MigrateXprojProjectFactory(procRunner);

            // Ensure that there's no project.json
            File.Delete(projectJson);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.False(migrator.BackupProject(backupDirectory, xproj, "XprojMigrationTests", logger));

            Assert.Equal(1, loggedMessages.Count);
            Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, loggedMessages[0].Level);
            Assert.Equal("XprojMigrationTests", loggedMessages[0].Project);
            Assert.Equal(projectJson, loggedMessages[0].File);
            Assert.Equal($"Failed to migrate XProj project XprojMigrationTests. Could not find project.json at {projectJson}.", loggedMessages[0].Message);
            Assert.Equal(0, Directory.EnumerateFiles(backupDirectory).Count());
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
