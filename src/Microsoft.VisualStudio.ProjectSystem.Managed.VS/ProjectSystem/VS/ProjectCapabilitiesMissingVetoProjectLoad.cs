// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Verifies that the project was loaded with the expected capabilities to catch
    ///     when the project type in the solution does not match the project itself, for
    ///     example when the user has renamed csproj -> vbproj without updating the project
    ///     type in the solution.
    /// </summary>
    [Export(typeof(IVetoProjectPreLoad))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal partial class ProjectCapabilitiesMissingVetoProjectLoad : IVetoProjectPreLoad
    {
        private static readonly ImmutableArray<ProjectType> s_projectTypes = GetProjectTypes();
        private readonly UnconfiguredProject _project;
        private readonly IProjectCapabilitiesService _projectCapabilitiesService;

        [ImportingConstructor]
        public ProjectCapabilitiesMissingVetoProjectLoad(UnconfiguredProject project, IProjectCapabilitiesService projectCapabilitiesService)
        {
            _project = project;
            _projectCapabilitiesService = projectCapabilitiesService;
        }

        public Task<bool> AllowProjectLoadAsync(bool isNewProject, ProjectConfiguration activeConfiguration, CancellationToken cancellationToken = default)
        {
            ProjectType? projectType = GetCurrentProjectType();
            if (projectType is null)    // Unrecognized, probably a Shared Project
                return TaskResult.True;

            foreach (string capability in projectType.Capabilities)
            {
                if (!_projectCapabilitiesService.Contains(capability))
                {
                    // Throw instead of returning false so that we can control message and the HRESULT
                    throw new COMException(string.Format(CultureInfo.CurrentCulture, Resources.ProjectLoadedWithWrongProjectType, _project.FullPath),
                                           HResult.Fail);
                }
            }

            return TaskResult.True;
        }

        private ProjectType? GetCurrentProjectType()
        {
            Assumes.True(!s_projectTypes.IsEmpty);

            string extension = Path.GetExtension(_project.FullPath);

            foreach (ProjectType projectType in s_projectTypes)
            {
                if (StringComparers.Paths.Equals(projectType.Extension, extension))
                {
                    return projectType;
                }
            }

            return null;
        }

        private static ImmutableArray<ProjectType> GetProjectTypes()
        {
            Assembly assembly = typeof(ProjectCapabilitiesMissingVetoProjectLoad).Assembly;

            return assembly.GetCustomAttributes<ProjectTypeRegistrationAttribute>()
                           .Select(a => new ProjectType('.' + a.DefaultProjectExtension, a.Capabilities!.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)))
                           .ToImmutableArray();
        }
    }
}
