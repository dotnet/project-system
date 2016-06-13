// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class PhysicalProjectTreeTests
    {
        [Fact]
        public void Constructor_NullAsTreeService_ThrowsArgumentNull()
        {
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());

            Assert.Throws<ArgumentNullException>("treeService", () => {
                new PhysicalProjectTree((Lazy<IProjectTreeService>)null, projectTreeProvider);
            });
        }

        [Fact]
        public void Constructor_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("treeProvider", () => {
                new PhysicalProjectTree(projectTreeService, (Lazy<IProjectTreeProvider>)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsTreeService_SetTreeServiceProperty()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider);

            Assert.Same(projectTreeService.Value, projectTree.TreeService);
        }

        [Fact]
        public void Constructor_ValueAsTreeProvider_SetTreeProviderProperty()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.Create());
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            
            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider);

            Assert.Same(projectTreeProvider.Value, projectTree.TreeProvider);
        }

        [Fact]
        public void Constructor_WhenTreeServiceCurrentTreeIsNull_SetsCurrentTreeToNull()
        {
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.ImplementCurrentTree(() => null));
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());
            
            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider);

            Assert.Null(projectTree.CurrentTree);
        }

        [Fact]
        public void Constructor_WhenTreeServiceCurrentTreeTreeIsNull_SetsCurrentTreeToNull()
        {
            var projectTreeServiceState = IProjectTreeServiceStateFactory.ImplementTree(() => null);
            var projectTreeService = new Lazy<IProjectTreeService>(() => IProjectTreeServiceFactory.ImplementCurrentTree(() => projectTreeServiceState));
            var projectTreeProvider = new Lazy<IProjectTreeProvider>(() => IProjectTreeProviderFactory.Create());

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider);

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

            var projectTree = new PhysicalProjectTree(projectTreeService, projectTreeProvider);

            Assert.Same(tree, projectTree.CurrentTree);
        }
    }
}
