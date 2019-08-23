// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Configuration;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the project file, and updates <see cref="IWorkspaceProjectContext.ProjectFilePath"/> 
    ///     and <see cref="IWorkspaceProjectContext.DisplayName"/>.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class ProjectFilePathAndDisplayNameEvaluationHandler : AbstractWorkspaceContextHandler, IProjectEvaluationHandler
    {
        private readonly ConfiguredProject _project;
        private readonly IImplicitlyActiveDimensionProvider _implicitlyActiveDimensionProvider;

        [ImportingConstructor]
        public ProjectFilePathAndDisplayNameEvaluationHandler(ConfiguredProject project, IImplicitlyActiveDimensionProvider implicitlyActiveDimensionProvider)
        {
            _project = project;
            _implicitlyActiveDimensionProvider = implicitlyActiveDimensionProvider;
        }

        public string ProjectEvaluationRule
        {
            get { return ConfigurationGeneral.SchemaName; }
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.MSBuildProjectFullPathProperty))
            {
                string projectFilePath = projectChange.After.Properties[ConfigurationGeneral.MSBuildProjectFullPathProperty];
                string displayName = GetDisplayName(projectFilePath);

                logger.WriteLine("DisplayName: {0}", displayName);
                logger.WriteLine("ProjectFilePath: {0}", projectFilePath);

                Context.ProjectFilePath = projectFilePath;
                Context.DisplayName = displayName;
            }
        }

        private string GetDisplayName(string projectFilePath)
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

            ProjectConfiguration configuration = _project.ProjectConfiguration;

            IEnumerable<string> dimensionNames = _implicitlyActiveDimensionProvider.GetImplicitlyActiveDimensions(configuration.Dimensions.Keys);

            string disambiguation = string.Join(", ", dimensionNames.Select(dimensionName => configuration.Dimensions[dimensionName]));
            if (disambiguation.Length == 0)
                return projectName;

            return $"{projectName} ({disambiguation})";
        }
    }
}
