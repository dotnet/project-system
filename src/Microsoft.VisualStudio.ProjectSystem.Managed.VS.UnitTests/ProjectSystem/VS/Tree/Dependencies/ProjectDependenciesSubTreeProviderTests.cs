// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.VisualStudio.Imaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class ProjectDependenciesSubTreeProviderTests
    {
        [Fact]
        public void ProjectDependenciesSubTreeProvider_CreateRootNode()
        {
            var provider = new TestableProjectDependenciesSubTreeProvider(null, null);

            var rootNode = provider.TestCreateRootNode();

            Assert.True(rootNode is SubTreeRootDependencyNode);
            Assert.True(rootNode.Flags.Contains(provider.ProjectSubTreeRootNodeFlags));
            Assert.Equal("Projects", rootNode.Caption);
            Assert.Equal(KnownMonikers.ApplicationGroup, rootNode.Icon);
            Assert.Equal(ProjectDependenciesSubTreeProvider.ProviderTypeString, rootNode.Id.ProviderType);
        }

        [Fact]
        public void ProjectDependenciesSubTreeProvider_GetDependencyNode()
        {
            // Arrange
            var projectPath = @"c:\myproject\project.csproj";

            // our provider under test nodes
            var myRootNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""ProviderUnderTest"",
        ""ItemSpec"": ""RootNodeUnderTest""
    }
}");

            var myTopNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""ProviderUnderTest"",
        ""ItemSpec"": ""MyTopNodeItemSpec1""
    }
}");

            var myDependencyNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""ProviderUnderTest"",
        ""ItemSpec"": ""MyDependencyNodeItemSpec1""
    }
}");
            myDependencyNode1.Id.ContextProject = "c:\\myproject\\project.csproj";

            myTopNode1.Children.Add(myDependencyNode1);
            myRootNode.Children.Add(myTopNode1);

            // other provider nodes
            var otherProviderRootNode = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyRootNode""
    }
}");

            var topNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec""
    }
}");
            var topNode2 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec/1.0.0""
    }
}");

            var topNode3 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyTopNodeItemSpec3/1.0.0""
    }
}");

            var childNode1 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec1.0""
    }
}");

            var childNode2 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec/1.0.0""
    }
}");

            var childNode3 = IDependencyNodeFactory.FromJson(@"
{
    ""Id"": {
        ""ProviderType"": ""MyProvider"",
        ""ItemSpec"": ""MyChildNodeItemSpec""
    }
}");
            topNode1.Children.Add(childNode1);
            topNode2.Children.Add(childNode3);
            topNode2.Children.Add(childNode2);
            otherProviderRootNode.AddChild(topNode1);
            otherProviderRootNode.AddChild(topNode2);
            otherProviderRootNode.AddChild(topNode3);

            var mockProvider = new IProjectDependenciesSubTreeProviderMock();
            mockProvider.RootNode = otherProviderRootNode;
            mockProvider.AddTestDependencyNodes(new[] { topNode1, topNode2, topNode3 });
            var mockProjectContextProvider = IDependenciesGraphProjectContextProviderFactory.Implement(
                                                projectPath, subTreeProviders: new[] { mockProvider });

            var provider = new TestableProjectDependenciesSubTreeProvider(unconfiguredProject:null,
                                                                          projectContextProvider: mockProjectContextProvider);
            provider.SetRootNode(myRootNode);

            // Act 
            var resultNode = provider.GetDependencyNode(myDependencyNode1.Id);                       

            // Assert
            Assert.Equal(resultNode.Id, myDependencyNode1.Id);
            Assert.Equal(3, resultNode.Children.Count);
        }

        [Fact]
        public void ProjectDependenciesSubTreeProvider_CreateDependencyNode()
        {
            // Arrange
            var projectPath = @"c:\mySolution\myproject\project.csproj";
            var testProjectPath = @"..\testProjectsubfolder\testproject.csproj";

            var mockUnconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectPath);

            var provider = new TestableProjectDependenciesSubTreeProvider(unconfiguredProject: mockUnconfiguredProject,
                                                                          projectContextProvider: null);

            // Act 
            var resultNode = provider.TestCreateDependencyNode(testProjectPath, "myItemType");

            Assert.Equal(resultNode.Id.ItemSpec, testProjectPath);
            Assert.Equal(resultNode.Id.ContextProject, 
                         Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath), testProjectPath)));
        }

        private class TestableProjectDependenciesSubTreeProvider : ProjectDependenciesSubTreeProvider
        {
            public TestableProjectDependenciesSubTreeProvider(
                        UnconfiguredProject unconfiguredProject,
                        IDependenciesGraphProjectContextProvider projectContextProvider)
                : base(unconfiguredProject, projectContextProvider)
            {
            }

            public IDependencyNode TestCreateRootNode()
            {
                return CreateRootNode();
            }

            public IDependencyNode TestCreateDependencyNode(string itemSpec,
                                                            string itemType,
                                                            int priority = 0,
                                                            IImmutableDictionary<string, string> properties = null,
                                                            bool resolved = true)
            {
                return CreateDependencyNode(itemSpec, itemType, priority, properties, resolved);
            }

            public void SetRootNode(IDependencyNode node)
            {
                RootNode = node;                
            }
        }
    }
}
