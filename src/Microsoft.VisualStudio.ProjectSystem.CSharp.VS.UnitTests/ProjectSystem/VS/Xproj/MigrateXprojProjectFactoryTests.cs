// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [Fact]
        public void MigrateXprojProjectFactory_MigrateOutput_LoggedCorrectly()
        {
            var projectDirectory = @"C:\Test";
            var xproj = @"C:\Test\XprojMigrationTests.xproj";

            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(pr =>
            {
                Assert.Equal("dotnet.exe", pr.FileName);
                Assert.Equal("migrate -s -p \"C:\\Test\" -x \"C:\\Test\\XprojMigrationTests.xproj\"", pr.Arguments);
            }, outputText: "Standard Output", errorText: "Standard Error");
            var migrator = new MigrateXprojProjectFactory(procRunner);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.True(migrator.MigrateProject(projectDirectory, xproj, "XprojMigrationTests", logger));
            Assert.Equal(2, loggedMessages.Count);
            Assert.Equal(new LogMessage
            {
                File = xproj,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL,
                Message = "Standard Output",
                Project = "XprojMigrationTests"
            }, loggedMessages[0]);
            Assert.Equal(new LogMessage
            {
                File = xproj,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_WARNING,
                Message = "Standard Error",
                Project = "XprojMigrationTests"
            }, loggedMessages[1]);
        }

        [Fact]
        public void MigrateXprojProjectFactory_MigrateError_ReturnsFalse()
        {
            var projectDirectory = @"C:\Test";
            var xproj = @"C:\Test\XprojMigrationTests.xproj";

            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(pr =>
            {
                Assert.Equal("dotnet.exe", pr.FileName);
                Assert.Equal("migrate -s -p \"C:\\Test\" -x \"C:\\Test\\XprojMigrationTests.xproj\"", pr.Arguments);
            }, exitCode: VSConstants.E_FAIL);
            var migrator = new MigrateXprojProjectFactory(procRunner);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            Assert.False(migrator.MigrateProject(projectDirectory, xproj, "XprojMigrationTests", logger));
            Assert.Equal(1, loggedMessages.Count);
            Assert.Equal(new LogMessage
            {
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Project = "XprojMigrationTests",
                File = xproj,
                Message = $"Failed to migrate XProj project XprojMigrationTests. 'dotnet migrate -s -p \"{projectDirectory}\" -x \"{xproj}\"' exited with error code {VSConstants.E_FAIL}."
            }, loggedMessages[0]);
        }

        [Fact]
        public void MigrateXprojProjectFactory_ValidCsproj_FindsCsproj()
        {
            var tuple = CreateTempProjectLocation();
            var xproj = tuple.Item2;
            var projectDir = tuple.Item4;
            var csproj = Path.Combine(projectDir, "XprojMigrationTests.csproj");

            File.Create(csproj).Dispose();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = new MigrateXprojProjectFactory(ProcessRunnerFactory.CreateRunner());

            Assert.Equal(csproj, migrator.GetCsproj(projectDir, "XprojMigrationTests", xproj, logger));
            Assert.Equal(0, loggedMessages.Count);
        }

        [Fact]
        public void MigrateXprojProjectFactory_NoCsproj_LogsError()
        {
            var tuple = CreateTempProjectLocation();
            var xproj = tuple.Item2;
            var projectDir = tuple.Item4;
            var csproj = Path.Combine(projectDir, "XprojMigrationTests.csproj");

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = new MigrateXprojProjectFactory(ProcessRunnerFactory.CreateRunner());

            Assert.Equal("", migrator.GetCsproj(projectDir, "XprojMigrationTests", xproj, logger));
            Assert.Equal(1, loggedMessages.Count);
            Assert.Equal(new LogMessage
            {
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Project = "XprojMigrationTests",
                File = xproj,
                Message = $"Expected to find migrated cpsroj in {projectDir}, but did not find any."
            }, loggedMessages[0]);
        }

        [Fact]
        public void MigrateXprojProjectFactory_ValidProject_IsUpgradable()
        {
            var tuple = CreateTempProjectLocation();
            var xproj = tuple.Item2;

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            int upgradeRequired;
            Guid newProjectFactory;
            uint capabilityFlags;

            var migrator = new MigrateXprojProjectFactory(ProcessRunnerFactory.CreateRunner());

            Assert.Equal(VSConstants.S_OK,
                migrator.UpgradeProject_CheckOnly(xproj, logger, out upgradeRequired, out newProjectFactory, out capabilityFlags));
            Assert.Equal((int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE, upgradeRequired);
            Assert.Equal(Guid.Parse(CSharpProjectSystemPackage.ProjectTypeGuid), newProjectFactory);
            Assert.Equal((uint)(__VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_BACKUPSUPPORTED | __VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_COPYBACKUP),
                capabilityFlags);
        }

        private Tuple<string, string, string, string> CreateTempProjectLocation()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"XprojMigrationTests_{Guid.NewGuid().ToString()}");
            Directory.CreateDirectory(directory);
            var backupDirectory = Path.Combine(directory, "backup");
            Directory.CreateDirectory(backupDirectory);
            var xproj = Path.Combine(directory, "XprojMigrationTests.xproj");
            File.Create(xproj).Dispose();
            var projectJson = Path.Combine(directory, "project.json");
            File.Create(projectJson).Dispose();

            return Tuple.Create(backupDirectory, xproj, projectJson, directory);
        }
    }
}
