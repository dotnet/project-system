// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;

using Moq;
using Moq.Protected;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class WorkspaceProjectContextProviderTests
    {
        [Fact]
        public async Task CreateProjectContextAsync_NullAsProject_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return provider.CreateProjectContextAsync((ConfiguredProject)null);
            });
        }

        [Fact]
        public async Task ReleaseProjectContextAsync_NullAsAccessor_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("accessor", () =>
            {
                return provider.ReleaseProjectContextAsync((IWorkspaceProjectContextAccessor)null);
            });
        }

        [Theory]
        [InlineData(
@"{
    ""Properties"": {
        ""LanguageServiceName"": ""CSharp"",
        ""TargetPath"": ""C:\\Target.dll""
    }
}")]

        [InlineData(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""TargetPath"": ""C:\\Target.dll""
    }
}")]
        [InlineData(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""LanguageServiceName"": ""CSharp"",
    }
}")]
        [InlineData(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": """",
        ""LanguageServiceName"": ""CSharp"",
        ""TargetPath"": ""C:\\Target.dll""
    }
}")]
        [InlineData(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""LanguageServiceName"": """",
        ""TargetPath"": ""C:\\Target.dll""
    }
}")]
        [InlineData(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""LanguageServiceName"": ""CSharp"",
        ""TargetPath"": """"
    }
}")]
        public async Task CreateProjectContextAsync_WhenEmptyOrMissingMSBuildProperties_ReturnsNull(string json)
        {
            var snapshot = IProjectRuleSnapshotFactory.FromJson(json);

            int callCount = 0;
            var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext((_, __, ___, ____, ______, _______) => { callCount++; return null; });
            var provider = CreateInstance(workspaceProjectContextFactory: workspaceProjectContextFactory, projectRuleSnapshot: snapshot);

            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");

            var result = await provider.CreateProjectContextAsync(project);

            Assert.Null(result);
            Assert.Equal(0, callCount);
        }

        [Theory] // Configurations          Project GUID                               Expected
        [InlineData("Debug",                "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "C:\\Project\\Project.csproj (Debug {72B509BD-C502-4707-ADFD-E2D43867CF45})")]
        [InlineData("Debug|AnyCPU",         "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "C:\\Project\\Project.csproj (Debug;AnyCPU {72B509BD-C502-4707-ADFD-E2D43867CF45})")]
        [InlineData("Debug|AnyCPU|net45",   "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "C:\\Project\\Project.csproj (Debug;AnyCPU;net45 {72B509BD-C502-4707-ADFD-E2D43867CF45})")]
        public async Task CreateProjectContextAsync_UniquelyIdentifiesContext(string configuration, string guid, string expected)
        {
            var projectGuidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(new Guid(guid));

            string result = null;
            var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext((_, id, ___, ____, ______, _______) => { result = id; return null; });
            var provider = CreateInstance(workspaceProjectContextFactory: workspaceProjectContextFactory, projectGuidService: projectGuidService);

            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(configuration);

            await provider.CreateProjectContextAsync(project);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CreateProjectContextAsync_PassesThroughDataToCreateProjectContext()
        {
            var projectRuleSnapshot = IProjectRuleSnapshotFactory.FromJson(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""LanguageServiceName"": ""CSharp"",
        ""TargetPath"": ""C:\\Target.dll""
    }
}");
            var projectGuid = new Guid("{72B509BD-C502-4707-ADFD-E2D43867CF45}");
            var projectGuidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(projectGuid);

            var hostObject = new object();
            var unconfiguredProject = UnconfiguredProjectFactory.Create(hostObject);

            string languageNameResult = null, projectFilePathResult = null, binOutputPathResult = null;
            Guid? projectGuidResult = null;
            object hierarchyResult = null;
            var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext((languageName, _, projectFilePath, guid, hierarchy, binOutputPath) => 
            {
                languageNameResult = languageName;
                projectFilePathResult = projectFilePath;
                projectGuidResult = guid;
                hierarchyResult = hierarchy;
                binOutputPathResult = binOutputPath;
                return null;
            });

            var provider = CreateInstance(project: unconfiguredProject, workspaceProjectContextFactory: workspaceProjectContextFactory, projectGuidService: projectGuidService);
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");

            await provider.CreateProjectContextAsync(project);

            Assert.Equal("CSharp", languageNameResult);
            Assert.Equal("C:\\Project\\Project.csproj", projectFilePathResult);
            Assert.Equal("C:\\Target.dll", binOutputPathResult);
            Assert.Equal(projectGuid, projectGuidResult.Value);
            Assert.Equal(hostObject, hierarchyResult);
        }

        [Fact]
        public async Task CreateProjectContextAsync_ReturnsContextWithLastDesignTimeBuildSucceededSetToFalse()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext((_, __, ___, ____, ______, _______) => context);
            var provider = CreateInstance(workspaceProjectContextFactory: workspaceProjectContextFactory);

            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");

            var result = await provider.CreateProjectContextAsync(project);

            Assert.NotNull(context);
            Assert.False(context.LastDesignTimeBuildSucceeded);
        }

        [Fact]
        public async Task CreateProjectContextAsync_WhenCreateProjectContextThrows_ReturnsNull()
        {
            var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext((_, __, ___, ____, ______, _______) => { throw new Exception(); });
            var provider = CreateInstance(workspaceProjectContextFactory: workspaceProjectContextFactory);

            var project = ConfiguredProjectFactory.ImplementProjectConfiguration("Debug|AnyCPU");

            var result = await provider.CreateProjectContextAsync(project);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReleaseProjectContextAsync_DisposesContext()
        {
            var provider = CreateInstance();

            int callCount = 0;
            var projectContext = IWorkspaceProjectContextMockFactory.ImplementDispose(() => callCount++);
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(projectContext);

            await provider.ReleaseProjectContextAsync(accessor);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ReleaseProjectContextAsync_WhenContextThrows_SwallowsException()
        {
            var provider = CreateInstance();

            var projectContext = IWorkspaceProjectContextMockFactory.ImplementDispose(() => throw new Exception());
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContext(projectContext);

            await provider.ReleaseProjectContextAsync(accessor);
        }

        private static WorkspaceProjectContextProvider CreateInstance(UnconfiguredProject project = null, IProjectThreadingService threadingService = null, IWorkspaceProjectContextFactory workspaceProjectContextFactory = null, ISafeProjectGuidService projectGuidService = null, IProjectRuleSnapshot projectRuleSnapshot = null)
        {
            projectRuleSnapshot = projectRuleSnapshot ?? IProjectRuleSnapshotFactory.FromJson(
@"{
    ""Properties"": {
        ""MSBuildProjectFullPath"": ""C:\\Project\\Project.csproj"",
        ""LanguageServiceName"": ""CSharp"",
        ""TargetPath"": ""C:\\Target.dll""
    }
}");

            var telemetryService = ITelemetryServiceFactory.Create();
            project = project ?? UnconfiguredProjectFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            workspaceProjectContextFactory = workspaceProjectContextFactory ?? IWorkspaceProjectContextFactoryFactory.Create();
            projectGuidService = projectGuidService ?? ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(Guid.NewGuid());

            var mock = new Mock<WorkspaceProjectContextProvider>(project, threadingService, projectGuidService, telemetryService, workspaceProjectContextFactory.AsLazy());
            mock.Protected().Setup<Task<IProjectRuleSnapshot>>("GetLatestSnapshotAsync", ItExpr.IsAny<ConfiguredProject>())
                            .ReturnsAsync(projectRuleSnapshot);

            return mock.Object;
        }
    }
}
