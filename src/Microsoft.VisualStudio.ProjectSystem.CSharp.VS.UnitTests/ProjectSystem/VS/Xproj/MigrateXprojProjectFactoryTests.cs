// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    public class MigrateXprojProjectFactoryTests
    {
        private const string SlnLocation = @"C:\Temp";
        private const string RootLocation = @"C:\Temp\XprojMigrationTests";
        private const string ProjectName = "XprojMigrationTests";
        private static readonly string XprojLocation = Path.Combine(RootLocation, $"{ProjectName}.xproj");
        private static readonly string XprojUserLocation = Path.Combine(RootLocation, $"{ProjectName}.xproj.user");
        private static readonly string ProjectJsonLocation = Path.Combine(RootLocation, "project.json");
        private static readonly string ProjectLockJsonLocation = Path.Combine(RootLocation, "project.lock.json");
        private static readonly string BackupLocation = Path.Combine(SlnLocation, "Backup");
        private static readonly string CsprojLocation = Path.Combine(RootLocation, $"{ProjectName}.csproj");
        private static readonly string LogFileLocation = Path.Combine(RootLocation, "asdf.1234");
        private static readonly string MigrateCommand = $"dotnet migrate --skip-backup -s -x \"{XprojLocation}\" \"{ProjectJsonLocation}\" -r \"{LogFileLocation}\" --format-report-file-json";
        private static readonly string GlobalJsonLocation = Path.Combine(SlnLocation, "global.json");

        [Fact]
        public void NullProcessRunner_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("runner", () => new MigrateXprojProjectFactory(
                null,
                new IFileSystemMock(),
                IServiceProviderFactory.Create(),
                GlobalJsonSetupFactory.Create()));
        }

        [Fact]
        public void NullFileSystem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("fileSystem", () => new MigrateXprojProjectFactory(
                ProcessRunnerFactory.CreateRunner(),
                null,
                IServiceProviderFactory.Create(),
                GlobalJsonSetupFactory.Create()));
        }

        [Fact]
        public void NullServiceProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new MigrateXprojProjectFactory(
                ProcessRunnerFactory.CreateRunner(),
                new IFileSystemMock(),
                null,
                GlobalJsonSetupFactory.Create()));
        }

        [Fact]
        public void NullGlobalJsonSetup_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("globalJsonSetup", () => new MigrateXprojProjectFactory(
                ProcessRunnerFactory.CreateRunner(),
                new IFileSystemMock(),
                IServiceProviderFactory.Create(),
                null));
        }

        [Fact]
        public void ValidArgs_BackupsCorrectly()
        {
            var procRunner = ProcessRunnerFactory.CreateRunner();
            var fileSystem = CreateFileSystem();
            var migrator = CreateInstance(procRunner, fileSystem);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.True(migrator.BackupProject(BackupLocation, XprojLocation, "XprojMigrationTests", logger));

            // We expect 2 informational messages about what files were backed up.
            AssertEx.CollectionLength(loggedMessages, 2);
            loggedMessages.ForEach(message =>
            {
                Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, message.Level);
                Assert.Equal("XprojMigrationTests", message.Project);
            });

            // The first message should be about the old xproj, the second about the project.json
            var xprojBackedUp = Path.Combine(BackupLocation, "XprojMigrationTests.xproj");
            var projectJsonBackedUp = Path.Combine(BackupLocation, "project.json");

            Assert.Equal(XprojLocation, loggedMessages[0].File);
            Assert.Equal(ProjectJsonLocation, loggedMessages[1].File);
            Assert.Equal(string.Format(VSResources.MigrationBackupFile, XprojLocation, xprojBackedUp), loggedMessages[0].Message);
            Assert.Equal(string.Format(VSResources.MigrationBackupFile, ProjectJsonLocation, projectJsonBackedUp), loggedMessages[1].Message);

            // Finally, assert that there actually are backup files in the backup directory
            var backedUpFiles = fileSystem.EnumerateFiles(BackupLocation, "*", SearchOption.TopDirectoryOnly);
            AssertEx.CollectionLength(backedUpFiles, 2);
            Assert.Contains(xprojBackedUp, backedUpFiles);
            Assert.Contains(projectJsonBackedUp, backedUpFiles);
        }

        [Fact]
        public void WithXprojUser_BackupsCorrectly()
        {
            var procRunner = ProcessRunnerFactory.CreateRunner();
            var fileSystem = CreateFileSystem(withXprojUser: true, withProjectLock: true);
            var migrator = CreateInstance(procRunner, fileSystem);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.True(migrator.BackupProject(BackupLocation, XprojLocation, "XprojMigrationTests", logger));

            // We expect 2 informational messages about what files were backed up.
            AssertEx.CollectionLength(loggedMessages, 3);
            loggedMessages.ForEach(message =>
            {
                Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, message.Level);
                Assert.Equal("XprojMigrationTests", message.Project);
            });

            // The first message should be about the old xproj, the second about the project.json
            var xprojBackedUp = Path.Combine(BackupLocation, "XprojMigrationTests.xproj");
            var xprojUserBackedUp = Path.Combine(BackupLocation, "XprojMigrationTests.xproj.user");
            var projectJsonBackedUp = Path.Combine(BackupLocation, "project.json");

            Assert.Equal(XprojLocation, loggedMessages[0].File);
            Assert.Equal(ProjectJsonLocation, loggedMessages[1].File);
            Assert.Equal(XprojUserLocation, loggedMessages[2].File);
            Assert.Equal(string.Format(VSResources.MigrationBackupFile, XprojLocation, xprojBackedUp), loggedMessages[0].Message);
            Assert.Equal(string.Format(VSResources.MigrationBackupFile, ProjectJsonLocation, projectJsonBackedUp), loggedMessages[1].Message);
            Assert.Equal(string.Format(VSResources.MigrationBackupFile, XprojUserLocation, xprojUserBackedUp), loggedMessages[2].Message);

            // Finally, assert that there actually are backup files in the backup directory
            var backedUpFiles = fileSystem.EnumerateFiles(BackupLocation, "*", SearchOption.TopDirectoryOnly);
            Assert.Equal(3, backedUpFiles.Count());
            Assert.Contains(xprojBackedUp, backedUpFiles);
            Assert.Contains(projectJsonBackedUp, backedUpFiles);
            Assert.Contains(xprojUserBackedUp, backedUpFiles);
        }


        [Fact]
        public void NonExistentProjectJson_DoesNotBackUp()
        {
            var procRunner = ProcessRunnerFactory.CreateRunner();
            var migrator = CreateInstance(procRunner, CreateFileSystem(false));

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.False(migrator.BackupProject(BackupLocation, XprojLocation, "XprojMigrationTests", logger));

            Assert.Single(loggedMessages);
            Assert.Equal((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, loggedMessages[0].Level);
            Assert.Equal("XprojMigrationTests", loggedMessages[0].Project);
            Assert.Equal(ProjectJsonLocation, loggedMessages[0].File);
            Assert.Equal(string.Format(VSResources.XprojMigrationFailedProjectJsonFileNotFound, ProjectName, ProjectJsonLocation), loggedMessages[0].Message);
        }

        [Fact]
        public void ValidPaths_CallMigrateCorrectly()
        {
            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(ProcessVerifier);
            var migrator = CreateInstance(procRunner, CreateFileSystem(false));

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            (string logFile, int exitCode) = migrator.MigrateProject(SlnLocation, RootLocation, XprojLocation, "XprojMigrationTests", logger);

            Assert.Equal(0, exitCode);
            Assert.Empty(loggedMessages);
        }

        [Fact]
        public void MigrateOutput_LoggedCorrectly()
        {
            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(ProcessVerifier, outputText: "Standard Output", errorText: "Standard Error");
            var migrator = CreateInstance(procRunner, CreateFileSystem(false));

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            Assert.Equal(0, migrator.MigrateProject(SlnLocation, RootLocation, XprojLocation, "XprojMigrationTests", logger).exitCode);
            AssertEx.CollectionLength(loggedMessages, 2);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL,
                Message = "Standard Output",
                Project = "XprojMigrationTests"
            }, loggedMessages[0]);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_WARNING,
                Message = "Standard Error",
                Project = "XprojMigrationTests"
            }, loggedMessages[1]);
        }

        [Fact]
        public void MigrateError_ReturnsErrorCode()
        {
            // Runner returns valid response, standard exit code
            var procRunner = ProcessRunnerFactory.ImplementRunner(ProcessVerifier, exitCode: VSConstants.E_FAIL);
            var migrator = CreateInstance(procRunner, CreateFileSystem(false));

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            (string logFile, int exitCode) = migrator.MigrateProject(SlnLocation, RootLocation, XprojLocation, "XprojMigrationTests", logger);
            Assert.Equal(VSConstants.E_FAIL, exitCode);
            Assert.Equal(LogFileLocation, logFile);
        }

        [Fact]
        public void ValidCsproj_FindsCsproj()
        {
            var fileSystem = CreateFileSystem();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            var (foundProjectFile, success) = migrator.LogReport(LogFileLocation, 0, ProjectName, XprojLocation, logger);
            Assert.Equal(CsprojLocation, foundProjectFile);
            Assert.True(success);
            Assert.Empty(loggedMessages);
        }

        [Fact]
        public void NoLogFile_LogsError()
        {
            var fileSystem = CreateFileSystem(withEntries: false);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            var (projectFile, success) = migrator.LogReport(LogFileLocation, VSConstants.E_FAIL, ProjectName, XprojLocation, logger);
            Assert.Equal(string.Empty, projectFile);
            Assert.False(success);
            AssertEx.CollectionLength(loggedMessages, 2);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = string.Format(VSResources.XprojMigrationFailedCannotReadReport, LogFileLocation),
                Project = ProjectName
            }, loggedMessages[0]);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = string.Format(VSResources.XprojMigrationGeneralFailure, ProjectName, MigrateCommand, VSConstants.E_FAIL),
                Project = ProjectName
            }, loggedMessages[1]);
        }

        [Fact]
        public void InvalidLogFile_LogsError()
        {
            var fileSystem = CreateFileSystem(withEntries: false);
            fileSystem.WriteAllText(LogFileLocation, string.Empty);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            var (projectFile, success) = migrator.LogReport(LogFileLocation, VSConstants.E_ABORT, ProjectName, XprojLocation, logger);
            Assert.Equal(string.Empty, projectFile);
            Assert.False(success);
            AssertEx.CollectionLength(loggedMessages, 2);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = string.Format(VSResources.XprojMigrationFailedCannotReadReport, LogFileLocation),
                Project = ProjectName
            }, loggedMessages[0]);
            Assert.Equal(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = string.Format(VSResources.XprojMigrationGeneralFailure, ProjectName, MigrateCommand, VSConstants.E_ABORT),
                Project = ProjectName
            }, loggedMessages[1]);
        }

        [Fact]
        public void LogWithErrors_MessagesAreLoggedToVs()
        {
            var fileSystem = CreateFileSystem(withEntries: false);
            var migrationReport = new MigrationReport(0, new List<ProjectMigrationReport>
            {
                new ProjectMigrationReport(false, string.Empty, RootLocation, ProjectName, new List<string>
                {
                    "Sample Warning 1",
                    "Sample Warning 2"
                }, new List<MigrationError> {
                    new MigrationError("asdf1234", "General Error", "An error has occurred"),
                    new MigrationError("fdsa4321", "Specific Error", "A different error has occurred")
                })
            });
            fileSystem.WriteAllText(LogFileLocation, JsonConvert.SerializeObject(migrationReport));

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            var (projectFile, success) = migrator.LogReport(LogFileLocation, VSConstants.E_ABORT, ProjectName, XprojLocation, logger);
            Assert.Equal(string.Empty, projectFile);
            Assert.False(success);
            Assert.Equal(5, loggedMessages.Count);
            Assert.Contains(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_WARNING,
                Message = "Sample Warning 1",
                Project = ProjectName
            }, loggedMessages);
            Assert.Contains(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_WARNING,
                Message = "Sample Warning 2",
                Project = ProjectName
            }, loggedMessages);
            Assert.Contains(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = "asdf1234::General Error: An error has occurred",
                Project = ProjectName
            }, loggedMessages);
            Assert.Contains(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = "fdsa4321::Specific Error: A different error has occurred",
                Project = ProjectName
            }, loggedMessages);
            Assert.Contains(new LogMessage
            {
                File = XprojLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_ERROR,
                Message = string.Format(VSResources.XprojMigrationGeneralFailure, ProjectName, MigrateCommand, VSConstants.E_ABORT),
                Project = ProjectName
            }, loggedMessages);
        }

        [Fact]
        public void ValidProject_IsUpgradable()
        {
            var fileSystem = CreateFileSystem();

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            Assert.Equal(VSConstants.S_OK,
                migrator.UpgradeProject_CheckOnly(XprojLocation, logger, out int upgradeRequired, out Guid newProjectFactory, out uint capabilityFlags));
            Assert.Equal((int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE, upgradeRequired);
            Assert.Equal(Guid.Parse(ProjectType.CSharp), newProjectFactory);
            Assert.Equal((uint)(__VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_BACKUPSUPPORTED | __VSPPROJECTUPGRADEVIAFACTORYFLAGS.PUVFF_COPYBACKUP),
                capabilityFlags);
        }

        [Fact]
        public void Cleanup_RemovesAllFiles()
        {
            var fileSystem = CreateFileSystem(withEntries: true, withXprojUser: true, withProjectLock: true);

            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            migrator.CleanupXproj(RootLocation, ProjectName);
            Assert.False(fileSystem.FileExists(XprojLocation));
            Assert.False(fileSystem.FileExists(ProjectJsonLocation));
            Assert.False(fileSystem.FileExists(XprojUserLocation));
            Assert.False(fileSystem.FileExists(ProjectLockJsonLocation));
        }

        [Fact]
        public void NoXprojUserOrProjectLock_CausesNoIssues()
        {
            var fileSystem = CreateFileSystem(withEntries: true, withXprojUser: false, withProjectLock: false);

            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem);

            migrator.CleanupXproj(RootLocation, ProjectName);
            Assert.False(fileSystem.FileExists(XprojLocation));
            Assert.False(fileSystem.FileExists(ProjectJsonLocation));
        }

        [Fact]
        public void GlobalJsonExists_BacksUpAndRemovesGlobalJson()
        {
            var fileSystem = CreateFileSystem(withEntries: true, withGlobalJson: true);
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(CreateSolutionInfo());
            var setup = GlobalJsonSetupFactory.Create(true);

            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem, globalJsonSetup: setup);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var globalJsonBackedUp = Path.Combine(BackupLocation, "global.json");

            migrator.BackupAndDeleteGlobalJson(SlnLocation, solution, BackupLocation, ProjectName, logger);
            Assert.True(fileSystem.FileExists(globalJsonBackedUp));
            Assert.Single(loggedMessages);
            Assert.Equal(new LogMessage
            {
                File = GlobalJsonLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL,
                Message = string.Format(VSResources.MigrationBackupFile, GlobalJsonLocation, globalJsonBackedUp),
                Project = ProjectName
            }, loggedMessages[0]);
            Mock.Get(setup).Verify(g => g.SetupRemoval(solution, It.IsAny<IServiceProvider>(), fileSystem));
        }

        [Fact]
        public void GlobalJsonHasSdkElement_ElementIsRemoved()
        {
            var fileSystem = CreateFileSystem(withEntries: true, withGlobalJson: true);
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(CreateSolutionInfo());
            var setup = GlobalJsonSetupFactory.Create(true);

            var migrator = CreateInstance(ProcessRunnerFactory.CreateRunner(), fileSystem, globalJsonSetup: setup);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);
            var globalJsonBackedUp = Path.Combine(BackupLocation, "global.json");

            fileSystem.WriteAllText(GlobalJsonLocation, @"
{
    ""sdk"": {
        ""version"": ""1.0.0-preview2-003121""
    }
}");

            migrator.BackupAndDeleteGlobalJson(SlnLocation, solution, BackupLocation, ProjectName, logger);
            Assert.True(fileSystem.FileExists(globalJsonBackedUp));
            Assert.Single(loggedMessages);
            Assert.Equal(new LogMessage
            {
                File = GlobalJsonLocation,
                Level = (uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL,
                Message = string.Format(VSResources.MigrationBackupFile, GlobalJsonLocation, globalJsonBackedUp),
                Project = ProjectName
            }, loggedMessages[0]);
            Mock.Get(setup).Verify(g => g.SetupRemoval(solution, It.IsAny<IServiceProvider>(), fileSystem));
            Assert.True(JToken.DeepEquals(new JObject(), JsonConvert.DeserializeObject<JObject>(fileSystem.ReadAllText(GlobalJsonLocation))));
        }

        [Fact]
        public void E2E_Works()
        {
            var fileSystem = CreateFileSystem(withEntries: true, withXprojUser: true, withProjectLock: true, withGlobalJson: true);
            var processRunner = ProcessRunnerFactory.ImplementRunner(pInfo =>
            {
                ProcessVerifier(pInfo);
                fileSystem.Create(CsprojLocation);
            });

            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(CreateSolutionInfo());
            var setup = GlobalJsonSetupFactory.Create(true);

            var loggedMessages = new List<LogMessage>();
            var logger = IVsUpgradeLoggerFactory.CreateLogger(loggedMessages);

            var migrator = CreateInstance(processRunner, fileSystem, solution, setup);

            Assert.Equal(VSConstants.S_OK, migrator.UpgradeProject(XprojLocation, 0, BackupLocation, out string outCsproj, logger, out int upgradeRequired, out Guid newProjectFactory));
            Assert.True(fileSystem.FileExists(CsprojLocation));
            Assert.True(fileSystem.FileExists(Path.Combine(BackupLocation, $"{ProjectName}.xproj")));
            Assert.True(fileSystem.FileExists(Path.Combine(BackupLocation, "project.json")));
            Assert.True(fileSystem.FileExists(Path.Combine(BackupLocation, $"{ProjectName}.xproj.user")));
            Assert.True(fileSystem.FileExists(Path.Combine(BackupLocation, $"global.json")));
            Assert.False(fileSystem.FileExists(XprojLocation));
            Assert.False(fileSystem.FileExists(ProjectJsonLocation));
            Assert.False(fileSystem.FileExists(XprojUserLocation));
            Assert.False(fileSystem.FileExists(ProjectLockJsonLocation));
            Assert.Equal(CsprojLocation, outCsproj);
            Assert.Equal((int)__VSPPROJECTUPGRADEVIAFACTORYREPAIRFLAGS.VSPUVF_PROJECT_ONEWAYUPGRADE, upgradeRequired);
            Assert.Equal(Guid.Parse(ProjectType.CSharp), newProjectFactory);
            Mock.Get(setup).Verify(g => g.SetupRemoval(solution, It.IsAny<IServiceProvider>(), fileSystem));
        }

        private MigrateXprojProjectFactory CreateInstance(ProcessRunner processRunner,
            IFileSystem fileSystem,
            IVsSolution solutionParam = null,
            GlobalJsonRemover.GlobalJsonSetup globalJsonSetup = null)
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var solution = solutionParam ?? IVsSolutionFactory.CreateWithSolutionDirectory(CreateSolutionInfo());
            var serviceProvider = IServiceProviderFactory.Create(typeof(SVsSolution), solution);
            var setup = globalJsonSetup ?? GlobalJsonSetupFactory.Create();

            var migrator = new MigrateXprojProjectFactory(processRunner, fileSystem, serviceProvider, setup);
            return migrator;
        }

        private void ProcessVerifier(ProcessStartInfo info)
        {
            Assert.Equal("dotnet.exe", info.FileName);
            Assert.Equal($"migrate --skip-backup -s -x \"{XprojLocation}\" \"{ProjectJsonLocation}\" -r \"{LogFileLocation}\" --format-report-file-json", info.Arguments);
            Assert.True(info.EnvironmentVariables.ContainsKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE"));
            Assert.Equal("true", info.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"]);
            Assert.Equal(SlnLocation, info.WorkingDirectory);
        }

        private FuncWithOutThreeArgs<string, string, string, int> CreateSolutionInfo(string directory = SlnLocation)
        {
            return (out string directoryArg, out string sln, out string suo) =>
            {
                directoryArg = directory;
                sln = null;
                suo = null;
                return VSConstants.S_OK;
            };
        }

        private IFileSystem CreateFileSystem(bool withEntries = true,
            MigrationReport report = null,
            bool withXprojUser = false,
            bool withProjectLock = false,
            bool withGlobalJson = false)
        {
            var fileSystem = new IFileSystemMock();
            if (withEntries)
            {
                fileSystem.CreateDirectory(RootLocation);
                fileSystem.CreateDirectory(BackupLocation);
                fileSystem.Create(XprojLocation);
                fileSystem.Create(ProjectJsonLocation);
                if (withXprojUser)
                {
                    fileSystem.Create(XprojUserLocation);
                }

                if (withProjectLock)
                {
                    fileSystem.Create(ProjectLockJsonLocation);
                }

                if (withGlobalJson)
                {
                    fileSystem.WriteAllText(GlobalJsonLocation, JsonConvert.SerializeObject(new object()));
                }
            }

            fileSystem.SetTempFile(LogFileLocation);

            if (withEntries)
            {
                report = report ?? new MigrationReport(1, new List<ProjectMigrationReport>()
                {
                    new ProjectMigrationReport(true, CsprojLocation, RootLocation, ProjectName, new List<string>(), new List<MigrationError>())
                });
                fileSystem.WriteAllText(LogFileLocation, JsonConvert.SerializeObject(report));
            }

            return fileSystem;
        }
    }
}
