// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Configuration;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class ProjectFilePathAndDisplayNameEvaluationHandlerTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Handle_WhenMSBuildProjectFullPathPropertyNotChanged_DoesNothing()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.ProjectFilePath = @"ProjectFilePath";
            context.DisplayName = "DisplayName";

            var handler = CreateInstance(context: context);

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true
                    }
                }
                """);

            Handle(context, handler, projectChange);

            Assert.Equal(@"ProjectFilePath", context.ProjectFilePath);
            Assert.Equal(@"DisplayName", context.DisplayName);
        }

        [Fact]
        public void Handle_WhenMSBuildProjectFullPathPropertyChanged_SetsProjectFilePath()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.ProjectFilePath = @"ProjectFilePath";

            var handler = CreateInstance(context: context);

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ "MSBuildProjectFullPath" ]
                    },
                    "After": { 
                        "Properties": {
                            "MSBuildProjectFullPath": "NewProjectFilePath"
                        }
                    }
                }
                """);
            Handle(context, handler, projectChange);

            Assert.Equal(@"NewProjectFilePath", context.ProjectFilePath);
        }

        [Fact]
        public void Handle_WhenMSBuildProjectFullPathPropertyChanged_SetsDisplayNameToFileNameWithoutExtension()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            var handler = CreateInstance(context: context);

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ "MSBuildProjectFullPath" ]
                    },
                    "After": { 
                        "Properties": {
                            "MSBuildProjectFullPath": "C:\\Project\\Project.csproj"
                        }
                    }
                }
                """);
            Handle(context, handler, projectChange);

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
            var handler = CreateInstance(implicitlyActiveDimensionProvider, context);

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ "MSBuildProjectFullPath" ]
                    },
                    "After": { 
                        "Properties": {
                            "MSBuildProjectFullPath": "C:\\Project\\Project.csproj"
                        }
                    }
                }
                """);

            Handle(context, handler, projectChange, configuration);

            Assert.Equal(expected, context.DisplayName);
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static ProjectFilePathAndDisplayNameEvaluationHandler CreateInstance(IImplicitlyActiveDimensionProvider? implicitlyActiveDimensionProvider = null, IWorkspaceProjectContext? context = null)
        {
            var project = UnconfiguredProjectFactory.Create();
            implicitlyActiveDimensionProvider ??= IImplicitlyActiveDimensionProviderFactory.Create();

            return new ProjectFilePathAndDisplayNameEvaluationHandler(project, implicitlyActiveDimensionProvider);
        }
    }
}
