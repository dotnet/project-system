// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class DteServicesTests
    {
        [Fact]
        public void Constructor_NullAsServiceProvider_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();

            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                new DteServices((IServiceProvider)null, projectVsServices);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectVsServices_ThrowsArgumentNull()
        {
            var serviceProvider = SVsServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("projectVsServices", () => {
                new DteServices(serviceProvider, (IUnconfiguredProjectVsServices)null);
            });
        }

        [Fact]
        public void DTE_WhenNotOnUIThread_Throws()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { throw new InvalidOperationException(); });
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(threadingServiceCreator: () => threadingService);

            var dteServices = CreateInstance(projectVsServices);

            Assert.Throws<InvalidOperationException>(() => {

                var ignored = dteServices.Dte;
            });
        }

        [Fact]
        public void Solution_WhenNotOnUIThread_Throws()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { throw new InvalidOperationException(); });
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(threadingServiceCreator: () => threadingService);

            var dteServices = CreateInstance(projectVsServices);

            Assert.Throws<InvalidOperationException>(() => {

                var ignored = dteServices.Solution;
            });
        }

        [Fact]
        public void Project_WhenNotOnUIThread_Throws()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { throw new InvalidOperationException(); });
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(threadingServiceCreator: () => threadingService);

            var dteServices = CreateInstance(projectVsServices);

            Assert.Throws<InvalidOperationException>(() => {

                var ignored = dteServices.Project;
            });
        }

        [Fact]
        public void DTE_ReturnsServiceProviderGetService()
        {
            var dte = DteFactory.Create();
            var serviceProvider = IServiceProviderFactory.Create(typeof(SDTE), dte);

            var dteServices = CreateInstance(serviceProvider);

            var result = dteServices.Dte;

            Assert.Same(dte, result);
        }

        [Fact]
        public void Solution_ReturnsServiceProviderGetService()
        {
            var solution = SolutionFactory.Create();
            var dte = DteFactory.ImplementSolution(() => solution);
            var serviceProvider = IServiceProviderFactory.Create(typeof(SDTE), dte);

            var dteServices = CreateInstance(serviceProvider);

            var result = dteServices.Solution;

            Assert.Same(solution, result);
        }

        [Fact]
        public void Project_ReturnsVsHierarchyGetProperty()
        {
            var project = ProjectFactory.Create();
            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.ExtObject, project);
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(hierarchyCreator: () => hierarchy, threadingServiceCreator: () => threadingService);

            var dteServices = CreateInstance(projectVsServices);

            var result = dteServices.Project;

            Assert.Same(project, result);
        }

        private DteServices CreateInstance(IUnconfiguredProjectVsServices projectVsServices)
        {
            return new DteServices(SVsServiceProviderFactory.Create(), projectVsServices);
        }

        private DteServices CreateInstance(IServiceProvider serviceProvider)
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(threadingServiceCreator: () => threadingService);

            return new DteServices(serviceProvider, projectVsServices);
        }
    }
}
