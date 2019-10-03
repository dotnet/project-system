// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class PhysicalProjectTreeTests
    {
        [Fact]
        public void Constructor_ValueAsTreeService_SetTreeServiceProperty()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Same(projectTreeService.Value, projectTree.TreeService);
        }

        [Fact]
        public void Constructor_ValueAsTreeProvider_SetTreeProviderProperty()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Same(projectTreeProvider.Value, projectTree.TreeProvider);
        }

        [Fact]
        public void Constructor_ValueAsTreeStorage_SetTreeStorageProperty()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Same(projectTreeStorage.Value, projectTree.TreeStorage);
        }

        [Fact]
        public void Constructor_WhenTreeServiceCurrentTreeIsNull_SetsCurrentTreeToNull()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.ImplementCurrentTree(() => null));
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Null(projectTree.CurrentTree);
        }

        [Fact]
        public void Constructor_WhenTreeServiceCurrentTreeTreeIsNull_SetsCurrentTreeToNull()
        {
            var projectTreeServiceState = IProjectTreeServiceStateFactory.ImplementTree(() => null);
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.ImplementCurrentTree(() => projectTreeServiceState));
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Null(projectTree.CurrentTree);
        }

        [Fact]
        public void Constructor_ValueAsTreeService_SetsCurrentTreeToTreeServiceCurrentTreeTree()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var projectTreeServiceState = IProjectTreeServiceStateFactory.ImplementTree(() => tree);
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.ImplementCurrentTree(() => projectTreeServiceState));
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            var projectTreeStorage = new Lazy<IPhysicalProjectTreeStorage>(() => IPhysicalProjectTreeStorageFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider, projectTreeStorage);

            Assert.Same(tree, projectTree.CurrentTree);
        }
    }
}
