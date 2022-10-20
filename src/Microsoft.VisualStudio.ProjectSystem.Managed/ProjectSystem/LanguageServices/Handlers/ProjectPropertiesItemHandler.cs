// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to project properties defined in the <c>LanguageService.xaml</c> rule,
    ///     and updates the language service via <see cref="IWorkspaceProjectContext.SetProperty(string, string)"/>.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    internal class ProjectPropertiesItemHandler : IWorkspaceUpdateHandler, IProjectEvaluationHandler
    {
        [ImportingConstructor]
        public ProjectPropertiesItemHandler(UnconfiguredProject project)
        {
        }

        public string ProjectEvaluationRule
        {
            get { return LanguageService.SchemaName; }
        }

        public void Handle(IWorkspaceProjectContext context, ProjectConfiguration projectConfiguration, IComparable version, IProjectChangeDescription projectChange, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            foreach (string name in projectChange.Difference.ChangedProperties)
            {
                string value = projectChange.After.Properties[name];

                // Is it a property we're specifically aware of?
                if (TryHandleSpecificProperties(context, name, value, logger))
                    continue;

                // Otherwise, just pass it through
                logger.WriteLine("{0}: {1}", name, value);
                context.SetProperty(name, value);
            }

            // NOTE: Roslyn treats "unset" as true, so always set it.
            context.IsPrimary = state.IsActiveConfiguration;
        }

        private static bool TryHandleSpecificProperties(IWorkspaceProjectContext context, string name, string value, IManagedProjectDiagnosticOutputService logger)
        {
            // The language service wants both the intermediate (bin\obj) and output (bin\debug)) paths
            // so that it can automatically hook up project-to-project references. It does this by matching the 
            // bin output path with the another project's /reference argument, if they match, then it automatically 
            // introduces a project reference between the two. We pass the intermediate path via the /out 
            // command-line argument and set via one of the other handlers, where as the latter is calculated via 
            // the TargetPath property and explicitly set on the context.

            if (StringComparers.PropertyNames.Equals(name, LanguageService.TargetPathProperty))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    logger.WriteLine("BinOutputPath: {0}", value);
                    context.BinOutputPath = value;
                }

                return true;
            }

            return false;
        }
    }
}
