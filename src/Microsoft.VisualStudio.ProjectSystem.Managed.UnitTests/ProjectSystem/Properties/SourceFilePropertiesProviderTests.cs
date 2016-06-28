// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Workspaces;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal class TestSourceFilePropertiesProvider : AbstractSourceFilePropertyProvider
    {
        public TestSourceFilePropertiesProvider(UnconfiguredProject unconfiguredProject, Workspace workspace, IProjectThreadingService threadingService) 
            : base(unconfiguredProject, workspace, threadingService)
        {
        }
    }

    [ProjectSystemTrait]
    public class SourceFilePropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullUnconfiguredProject_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () =>
            {
                new TestSourceFilePropertiesProvider(null, WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullWorkspace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("workspace", () =>
            {
                new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), null, IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullThreadingFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), null);
            });
        }

        [Fact]
        public void DefaultProjectPath()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: "D:\\TestFile"), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Equal(provider.DefaultProjectPath, "D:\\TestFile");
        }

        [Fact]
        public void GetItemProperties_ThrowsInvalidOperationException()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Throws<InvalidOperationException>(() => provider.GetItemProperties(null, null));
        }

        [Fact]
        public void GetItemTypeProperties_ThrowsInvalidOperationException()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Throws<InvalidOperationException>(() => provider.GetItemTypeProperties(null));
        }

        [Fact]
        public void GetProperties_NotNull()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            var properties = provider.GetProperties(null, null, null);
            Assert.NotNull(properties);
        }

        [Theory]
        [InlineData(@"
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]
", "ComVisible", "true")]
        public async void SourceFileProperties_GetEvalutedProperty(string code, string propertyName, string expectedValue)
        {
            var workspace = WorkspaceFactory.Create(code);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: projectFilePath), workspace, IProjectThreadingServiceFactory.Create());

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }
    }
}
