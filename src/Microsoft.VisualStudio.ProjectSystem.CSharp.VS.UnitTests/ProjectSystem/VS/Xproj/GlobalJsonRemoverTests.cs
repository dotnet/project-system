// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [ProjectSystemTrait]
    public class GlobalJsonRemoverTests
    {
        private const string Directory = @"C:\Temp";

        [Fact]
        public void InvalidServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new GlobalJsonRemover(null, IFileSystemFactory.Create()));
        }

        [Fact]
        public void NullFileSystem_Throws()
        {
            Assert.Throws<ArgumentNullException>("fileSystem", () => new GlobalJsonRemover(IServiceProviderFactory.Create(), null));
        }

        [Fact]
        public void RemovesJson_WhenExists()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var globalJsonPath = Path.Combine(Directory, "global.json");
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(DirectoryInfoCallback);
            var projectItem = EnvDTE.ProjectItemFactory.Create();
            var dteSolution = SolutionFactory.ImplementFindProjectItem(path =>
            {
                Assert.Equal(globalJsonPath, path);
                return projectItem;
            });
            var dte = DteFactory.ImplementSolution(() => dteSolution);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(t =>
            {
                if (typeof(SVsSolution) == t)
                {
                    return solution;
                }

                if (typeof(DTE) == t)
                {
                    return dte;
                }

                Assert.False(true);
                throw new InvalidOperationException();
            });

            var fileSystem = IFileSystemFactory.Create();
            fileSystem.Create(globalJsonPath);

            var remover = new GlobalJsonRemover(serviceProvider, fileSystem);
            GlobalJsonRemover.Remover = remover;
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
            Mock.Get(projectItem).Verify(p => p.Delete(), Times.Once);
            Assert.False(fileSystem.FileExists(globalJsonPath));
        }

        [Fact]
        public void NoJson_DoesntCrash()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(DirectoryInfoCallback);
            var dteSolution = SolutionFactory.ImplementFindProjectItem(path =>
            {
                Assert.Equal(Path.Combine(Directory, "global.json"), path);
                return null;
            });
            var dte = DteFactory.ImplementSolution(() => dteSolution);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(t =>
            {
                if (typeof(SVsSolution) == t)
                {
                    return solution;
                }

                if (typeof(DTE) == t)
                {
                    return dte;
                }

                Assert.False(true);
                throw new InvalidOperationException();
            });

            var remover = new GlobalJsonRemover(serviceProvider, IFileSystemFactory.Create());
            GlobalJsonRemover.Remover = remover;
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
        }

        [Fact]
        public void AfterRemoval_UnadvisesEvents()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var globalJsonPath = Path.Combine(Directory, "global.json");
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(DirectoryInfoCallback);
            var projectItem = EnvDTE.ProjectItemFactory.Create();
            var dteSolution = SolutionFactory.ImplementFindProjectItem(path =>
            {
                Assert.Equal(globalJsonPath, path);
                return projectItem;
            });
            var dte = DteFactory.ImplementSolution(() => dteSolution);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(t =>
            {
                if (typeof(SVsSolution) == t)
                {
                    return solution;
                }

                if (typeof(DTE) == t)
                {
                    return dte;
                }

                Assert.False(true);
                throw new InvalidOperationException();
            });

            var fileSystem = IFileSystemFactory.Create();
            fileSystem.Create(globalJsonPath);

            var remover = new GlobalJsonRemover(serviceProvider, fileSystem)
            {
                SolutionCookie = 1234
            };
            GlobalJsonRemover.Remover = remover;
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
            Mock.Get(solution).Verify(s => s.UnadviseSolutionEvents(1234), Times.Once);
        }

        [Fact]
        public void GlobaJsonSetup_NoExistingRemover_RegistersForAdvise()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            GlobalJsonRemover.Remover = null;

            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(1234);
            var setupOccurred = new GlobalJsonRemover.GlobalJsonSetup().SetupRemoval(solution, IServiceProviderFactory.Create(), IFileSystemFactory.Create());
            Assert.True(setupOccurred);
            Assert.Equal(1234u, GlobalJsonRemover.Remover.SolutionCookie);
        }

        [Fact]
        public void GlobalJsonSetup_ExistingRemover_ReturnsSameRemover()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var remover = new GlobalJsonRemover(IServiceProviderFactory.Create(), IFileSystemFactory.Create());
            GlobalJsonRemover.Remover = remover;
            Assert.False(new GlobalJsonRemover.GlobalJsonSetup().SetupRemoval(IVsSolutionFactory.Create(),
                IServiceProviderFactory.Create(), IFileSystemFactory.Create()));
            Assert.True(ReferenceEquals(remover, GlobalJsonRemover.Remover));
        }

        private int DirectoryInfoCallback(out string directory, out string solutionFile, out string opts)
        {
            directory = Directory;
            solutionFile = null;
            opts = null;
            return VSConstants.S_OK;
        }
    }
}
