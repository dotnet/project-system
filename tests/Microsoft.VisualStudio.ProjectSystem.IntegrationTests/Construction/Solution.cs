// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Defines a <c>.sln</c> file to be created when using <see cref="ProjectLayoutTestBase"/>.
    /// </summary>
    public sealed class Solution : IEnumerable
    {
        private readonly List<Project> _projects = new();

        public IEnumerable<Project> Projects => _projects;

        public GlobalJson? GlobalJson { get; private set; }

        public void Save(string path)
        {
            using var stream = File.OpenWrite(path);
            using var writer = new StreamWriter(stream, Encoding.ASCII);

            writer.WriteLine();
            writer.WriteLine(
                """
                Microsoft Visual Studio Solution File, Format Version 12.00
                # Visual Studio 15
                VisualStudioVersion = 15.0.28010.2019
                MinimumVisualStudioVersion = 10.0.40219.1
                """);

            foreach (var project in _projects)
            {
                writer.WriteLine(
                    $"""
                    Project("{project.ProjectTypeGuid:B}") = "{project.ProjectName}", "{project.RelativeProjectFilePath}", "{project.ProjectGuid:B}"
                    EndProject
                    """);
            }

            writer.WriteLine(
                """
                Global
                    GlobalSection(SolutionConfigurationPlatforms) = preSolution
                        Debug|Any CPU = Debug|Any CPU
                        Release|Any CPU = Release|Any CPU
                    EndGlobalSection
                    GlobalSection(ProjectConfigurationPlatforms) = postSolution
                """);

            foreach (var project in _projects)
            {
                writer.WriteLine(
                $"""
                        {project.ProjectGuid:B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                        {project.ProjectGuid:B}.Debug|Any CPU.Build.0 = Debug|Any CPU
                        {project.ProjectGuid:B}.Release|Any CPU.ActiveCfg = Release|Any CPU
                        {project.ProjectGuid:B}.Release|Any CPU.Build.0 = Release|Any CPU
                """);
            }

            writer.WriteLine(
                $"""
                    EndGlobalSection
                    GlobalSection(SolutionProperties) = preSolution
                        HideSolutionNode = FALSE
                    EndGlobalSection
                    GlobalSection(ExtensibilityGlobals) = postSolution
                        SolutionGuid = {Guid.NewGuid():B}
                    EndGlobalSection
                EndGlobal
                """);
        }

        public void Add(Project project) => _projects.Add(project);

        public void Add(GlobalJson globalJson) => GlobalJson = globalJson;

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
