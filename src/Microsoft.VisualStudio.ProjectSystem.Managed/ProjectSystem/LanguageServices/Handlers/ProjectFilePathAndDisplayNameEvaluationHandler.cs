// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Configuration;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the project file, and updates <see cref="IWorkspaceProjectContext.ProjectFilePath"/>
    ///     and <see cref="IWorkspaceProjectContext.DisplayName"/>.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    internal class ProjectFilePathAndDisplayNameEvaluationHandler : IWorkspaceUpdateHandler, IProjectEvaluationHandler
    {
        private readonly UnconfiguredProject _project;
        private readonly IImplicitlyActiveDimensionProvider _implicitlyActiveDimensionProvider;

        [ImportingConstructor]
        public ProjectFilePathAndDisplayNameEvaluationHandler(UnconfiguredProject project, IImplicitlyActiveDimensionProvider implicitlyActiveDimensionProvider)
        {
            _project = project;
            _implicitlyActiveDimensionProvider = implicitlyActiveDimensionProvider;
        }

        public string ProjectEvaluationRule
        {
            get { return ConfigurationGeneral.SchemaName; }
        }

        public void Handle(IWorkspaceProjectContext context, ProjectConfiguration projectConfiguration, IComparable version, IProjectChangeDescription projectChange, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.MSBuildProjectFullPathProperty))
            {
                string projectFilePath = projectChange.After.Properties[ConfigurationGeneral.MSBuildProjectFullPathProperty];
                string displayName = GetDisplayName(projectFilePath, projectConfiguration);

                logger.WriteLine("DisplayName: {0}", displayName);
                logger.WriteLine("ProjectFilePath: {0}", projectFilePath);

                context.ProjectFilePath = projectFilePath;
                context.DisplayName = displayName;
            }
        }

        private string GetDisplayName(string projectFilePath, ProjectConfiguration projectConfiguration)
        {
            // Calculate the display name to use for the editor context switch and project column
            // in the Error List.
            //
            // When multi-targeting, we want to include the implicit dimension values in 
            // the name to disambiguate it from other contexts in the same project. For example:
            //
            // ClassLibrary (net45)
            // ClassLibrary (net46)

            string projectName = Path.GetFileNameWithoutExtension(projectFilePath);

            IEnumerable<string> dimensionNames = _implicitlyActiveDimensionProvider.GetImplicitlyActiveDimensions(projectConfiguration.Dimensions.Keys);

            string disambiguation = string.Join(", ", dimensionNames.Select(dimensionName => projectConfiguration.Dimensions[dimensionName]));
            if (disambiguation.Length == 0)
                return projectName;

            return $"{projectName} ({disambiguation})";
        }
    }
}
