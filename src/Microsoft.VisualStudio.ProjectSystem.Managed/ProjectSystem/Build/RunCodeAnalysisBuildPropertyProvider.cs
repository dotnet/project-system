// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Build property provider for CodeAnalysis related properties for solution build.
    /// </summary>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: false)]
    [Export(typeof(RunCodeAnalysisBuildPropertyProvider))]
    [AppliesTo(ProjectCapability.CodeAnalysis)]
    internal class RunCodeAnalysisBuildPropertyProvider : StaticGlobalPropertiesProviderBase
    {
        private const string RunCodeAnalysisOnceBuildPropertyName = "RunCodeAnalysisOnce";
        private const string CodeAnalysisProjectFullPathBuildPropertyName = "CodeAnalysisProjectFullPath";

        private bool _runCodeAnalysisOnce;
        private string _projectFullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunCodeAnalysisBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal RunCodeAnalysisBuildPropertyProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public void EnableRunCodeAnalysisOnBuild(string projectFullPath)
        {
            _runCodeAnalysisOnce = true;
            _projectFullPath = projectFullPath;
        }

        public void DisableRunCodeAnalysisOnBuild()
        {
            _runCodeAnalysisOnce = false;
            _projectFullPath = null;
        }

        /// <summary>
        /// Gets the set of global properties that should apply to the project(s) in this scope.
        /// </summary>
        /// <value>A map whose keys are case insensitive.  Never null, but may be empty.</value>
        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            IImmutableDictionary<string, string> properties = Empty.PropertiesMap;

            if (_runCodeAnalysisOnce && !string.IsNullOrEmpty(_projectFullPath))
            {
                properties = properties.Add(RunCodeAnalysisOnceBuildPropertyName, "true")
                    .Add(CodeAnalysisProjectFullPathBuildPropertyName, _projectFullPath);
            }

            return Task.FromResult(properties);
        }
    }
}