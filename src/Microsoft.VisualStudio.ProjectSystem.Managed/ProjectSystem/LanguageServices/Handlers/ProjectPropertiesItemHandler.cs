// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the project and makes sure the language service is aware of them.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class ProjectPropertiesItemHandler : IEvaluationHandler
    {
        private IWorkspaceProjectContext _context;

        [ImportingConstructor]
        public ProjectPropertiesItemHandler(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));
        }

        public string EvaluationRule
        {
            get { return ConfigurationGeneral.SchemaName; }
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            if (_context != null)
                throw new InvalidOperationException();

            _context = context;
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            if (_context == null)
                throw new InvalidOperationException();

            // The language service wants both the intermediate (bin\obj) and output (bin\debug)) paths
            // so that it can automatically hook up project-to-project references. It does this by matching the 
            // bin output path with the another project's /reference argument, if they match, then it automatically 
            // introduces a project reference between the two. We pass the intermediate path via the /out 
            // command-line argument and set via one of the other handlers, where as the latter is calculated via 
            // the TargetPath property and explictly set on the context.

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetPathProperty))
            {
                string newBinOutputPath = projectChange.After.Properties[ConfigurationGeneral.TargetPathProperty];
                if (!string.IsNullOrEmpty(newBinOutputPath))
                {
                    logger.WriteLine("BinOutputPath: {0}", newBinOutputPath);
                    _context.BinOutputPath = newBinOutputPath;
                }
            }
        }
    }
}
