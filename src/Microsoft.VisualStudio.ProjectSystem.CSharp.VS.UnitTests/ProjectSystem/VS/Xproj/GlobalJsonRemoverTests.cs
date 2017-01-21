// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [ProjectSystemTrait]
    public class GlobalJsonRemoverTests
    {
        private const string Directory = @"C:\Temp";

        [Fact]
        public void GlobalJsonRemover_InvalidServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new GlobalJsonRemover(null));
        }

        [Fact]
        public void GlobalJsonRemover_RemovesJson_WhenExists()
        {
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(DirectoryInfoCallback);
            var projectItem = ProjectItemFactory.Create();
            var dteSolution = SolutionFactory.ImplementFindProjectItem(path =>
            {
                Assert.Equal(Path.Combine(Directory, "global.json"), path);
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

            var remover = new GlobalJsonRemover(serviceProvider);
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
            Mock.Get(projectItem).Verify(p => p.Remove(), Times.Once);
        }

        [Fact]
        public void GlobalJsonRemover_NoJson_DoesntCrash()
        {
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

            var remover = new GlobalJsonRemover(serviceProvider);
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
        }

        [Fact]
        public void GlobalJsonRemover_AfterRemoval_UnadvisesEvents()
        {
            var solution = IVsSolutionFactory.CreateWithSolutionDirectory(DirectoryInfoCallback);
            var projectItem = ProjectItemFactory.Create();
            var dteSolution = SolutionFactory.ImplementFindProjectItem(path =>
            {
                Assert.Equal(Path.Combine(Directory, "global.json"), path);
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

            var remover = new GlobalJsonRemover(serviceProvider)
            {
                SolutionCookie = 1234
            };
            Assert.Equal(VSConstants.S_OK, remover.OnAfterOpenSolution(null, 0));
            Mock.Get(solution).Verify(s => s.UnadviseSolutionEvents(1234), Times.Once);
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
