// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class OutputTypeCheckerTest
    {
        private readonly string _ProjectFile = @"c:\test\project\project.csproj";

        [Fact]
        public async Task OutputTypeChecker_False_IsLibraryAsyncWhenEvaluationFails()
        {
            OutputTypeChecker outputTypeChecker = CreateFailedOutputTypeChecker();

            Assert.False(await outputTypeChecker.IsLibraryAsync());
        }

        [Fact]
        public async Task OutputTypeChecker_True_IsLibraryAsync()
        {
            OutputTypeChecker outputTypeChecker = CreateOutputTypeChecker(ConfigurationGeneral.OutputTypeValues.Library);

            Assert.True(await outputTypeChecker.IsLibraryAsync());
        }

        [Fact]
        public async Task OutputTypeChecker_False_IsLibraryAsync()
        {
            OutputTypeChecker outputTypeChecker = CreateOutputTypeChecker(ConfigurationGeneral.OutputTypeValues.Exe);

            Assert.False(await outputTypeChecker.IsLibraryAsync());
        }

        [Fact]
        public async Task OutputTypeChecker_True_IsConsoleAsync()
        {
            OutputTypeChecker outputTypeChecker = CreateOutputTypeChecker(ConfigurationGeneral.OutputTypeValues.Exe);

            Assert.True(await outputTypeChecker.IsConsoleAsync());
        }

        [Fact]
        public async Task OutputTypeChecker_False_IsConsoleAsync()
        {
            OutputTypeChecker outputTypeChecker = CreateOutputTypeChecker(ConfigurationGeneral.OutputTypeValues.Library);

            Assert.False(await outputTypeChecker.IsConsoleAsync());
        }

        private OutputTypeChecker CreateFailedOutputTypeChecker()
        {
            var projectProperties = ProjectPropertiesFactory.CreateEmpty();

            return new OutputTypeChecker2(projectProperties);
        }

        private OutputTypeChecker CreateOutputTypeChecker(string outputType)
        {
            var project = UnconfiguredProjectFactory.Create(fullPath: _ProjectFile);

            var outputTypeEnum = new PageEnumValue(new EnumValue { Name = outputType });
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputTypeProperty, outputTypeEnum);
            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            return new OutputTypeChecker(projectProperties);
        }

        internal class OutputTypeChecker2 : OutputTypeChecker
        {
            public OutputTypeChecker2(ProjectProperties properties) : base(properties)
            {
            }

            public override Task<IEnumValue?> GetEvaluatedOutputTypeAsync()
            {
                // Evaluation fails
                return Task.FromResult<IEnumValue?>(null);
            }
        }
    }
}
