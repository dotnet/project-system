// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the project and makes sure the language service is aware of them.
    /// </summary>
    [Export(typeof(IEvaluationHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class ProjectPropertiesItemHandler : IEvaluationHandler
    {
        [ImportingConstructor]
        public ProjectPropertiesItemHandler()
        {
        }

        public string EvaluationRuleName
        {
            get { return ConfigurationGeneral.SchemaName; }
        }

        public void Handle(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(projectChange, nameof(projectChange));

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.ProjectGuidProperty))
            {
                if (Guid.TryParse(projectChange.After.Properties[ConfigurationGeneral.ProjectGuidProperty], out Guid result))
                {
                    context.Guid = result;
                }
            }

            // The language service wants both the intermediate (bin\obj) and output (bin\debug)) paths
            // so that it can automatically hook up project-to-project references. It does this by matching the 
            // bin output path with the another project's /reference argument, if they match, then it automatically 
            // introduces a project reference between the two. We pass the intermediate path via the /out 
            // command-line argument and set via one of the other handlers, where as the latter is calculated via 
            // the TargetPath property and explictly set on the context.

            if (projectChange.Difference.ChangedProperties.Contains(ConfigurationGeneral.TargetPathProperty))
            {
                var newBinOutputPath = projectChange.After.Properties[ConfigurationGeneral.TargetPathProperty];
                if (!string.IsNullOrEmpty(newBinOutputPath))
                {
                    context.BinOutputPath = newBinOutputPath;
                }
            }
        }

        public void OnContextReleasedAsync(IWorkspaceProjectContext context)
        {
        }
    }
}
