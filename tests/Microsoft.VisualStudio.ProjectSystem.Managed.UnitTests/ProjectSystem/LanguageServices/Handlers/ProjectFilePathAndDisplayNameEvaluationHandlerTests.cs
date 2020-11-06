// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Configuration;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class ProjectFilePathAndDisplayNameEvaluationHandlerTests : EvaluationHandlerTestBase
    {
        [Theory]
        [InlineData("true", "false")]
        [InlineData("false", "true")]
        public void Handle_WhenMSBuildProjectFullPathPropertyNotChanged_DoesNothing(string diffContains, string afterContains)
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.ProjectFilePath = @"ProjectFilePath";
            context.DisplayName = "DisplayName";

            var handler = CreateInstance(context: context);
            var projectChange = SetupProjectChanges(bool.Parse(diffContains), "C:\\Project\\Project.csproj", bool.Parse(afterContains));

            Handle(handler, projectChange);

            Assert.Equal(@"ProjectFilePath", context.ProjectFilePath);
            Assert.Equal(@"DisplayName", context.DisplayName);
        }

        [Fact]
        public void Handle_WhenMSBuildProjectFullPathPropertyChanged_SetsProjectFilePath()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.ProjectFilePath = @"ProjectFilePath";

            var handler = CreateInstance(context: context);
            var projectChange = SetupProjectChanges(true, "NewProjectFilePath", true);

            Handle(handler, projectChange);

            Assert.Equal(@"NewProjectFilePath", context.ProjectFilePath);
        }

        [Fact]
        public void Handle_WhenMSBuildProjectFullPathPropertyChanged_SetsDisplayNameToFileNameWithoutExtension()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            var handler = CreateInstance(context: context);
            var projectChange = SetupProjectChanges(true, "C:\\Project\\Project.csproj", true);

            Handle(handler, projectChange);

            Assert.Equal(@"Project", context.DisplayName);
        }

        [Theory] // Dimension Names                             Dimension Values       Implicit Dimension Names,                 Expected
        [InlineData("Configuration",                            "Debug",               "",                                       "Project")]
        [InlineData("Configuration",                            "Debug",               "Configuration",                          "Project (Debug)")]
        [InlineData("Configuration|Platform",                   "Debug|AnyCPU",        "",                                       "Project")]
        [InlineData("Configuration|Platform",                   "Debug|AnyCPU",        "Configuration",                          "Project (Debug)")]
        [InlineData("Configuration|Platform",                   "Debug|AnyCPU",        "Configuration|Platform",                 "Project (Debug, AnyCPU)")]
        [InlineData("Configuration|Platform|TargetFramework",   "Debug|AnyCPU|net45",  "",                                       "Project")]
        [InlineData("Configuration|Platform|TargetFramework",   "Debug|AnyCPU|net45",  "Configuration",                          "Project (Debug)")]
        [InlineData("Configuration|Platform|TargetFramework",   "Debug|AnyCPU|net45",  "Configuration|Platform",                 "Project (Debug, AnyCPU)")]
        [InlineData("Configuration|Platform|TargetFramework",   "Debug|AnyCPU|net45",  "Configuration|Platform|TargetFramework", "Project (Debug, AnyCPU, net45)")]
        [InlineData("Configuration|Platform|TargetFramework",   "Debug|AnyCPU|net45",  "TargetFramework",                        "Project (net45)")]
        public void Handle_WhenMSBuildProjectFullPathPropertyChangedAndMultitargeting_IncludeDimensionValuesInDisplayName(string dimensionNames, string dimensionValues, string implicitDimensionNames, string expected)
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            var implicitlyActiveDimensionProvider = IImplicitlyActiveDimensionProviderFactory.ImplementGetImplicitlyActiveDimensions(n => implicitDimensionNames.SplitReturningEmptyIfEmpty('|'));
            var configuration = ProjectConfigurationFactory.Create(dimensionNames, dimensionValues);
            var handler = CreateInstance(configuration, implicitlyActiveDimensionProvider, context);
            var projectChange = SetupProjectChanges(true, "C:\\Project\\Project.csproj", true);

            Handle(handler, projectChange);

            Assert.Equal(expected, context.DisplayName);
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static ProjectFilePathAndDisplayNameEvaluationHandler CreateInstance(ProjectConfiguration? configuration = null, IImplicitlyActiveDimensionProvider? implicitlyActiveDimensionProvider = null, IWorkspaceProjectContext? context = null)
        {
            var project = ConfiguredProjectFactory.ImplementProjectConfiguration(configuration ?? ProjectConfigurationFactory.Create("Debug|AnyCPU"));
            implicitlyActiveDimensionProvider ??= IImplicitlyActiveDimensionProviderFactory.Create();

            var handler = new ProjectFilePathAndDisplayNameEvaluationHandler(project, implicitlyActiveDimensionProvider);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }

        private static IProjectChangeDescription SetupProjectChanges(bool containsDifference, string msBuildProjectFullPath, bool afterContainsChange)
        {
            var projectChangeMock = new Mock<IProjectChangeDescription>();
            projectChangeMock.Setup(c => c.Difference.ChangedProperties.Contains(ConfigurationGeneral.MSBuildProjectFullPathProperty)).Returns(containsDifference);

            projectChangeMock.Setup(c => c.After.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out msBuildProjectFullPath)).Returns(afterContainsChange);

            return projectChangeMock.Object;
        }
    }
}
