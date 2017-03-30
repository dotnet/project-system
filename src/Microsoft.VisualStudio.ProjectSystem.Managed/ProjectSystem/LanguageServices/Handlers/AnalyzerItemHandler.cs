// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;Analyzer/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceCommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class AnalyzerItemHandler : ILanguageServiceCommandLineHandler
    {
        [ImportingConstructor]
        public AnalyzerItemHandler(UnconfiguredProject project)
        {
        }

        public void Handle(CommandLineArguments added, CommandLineArguments removed, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineAnalyzerReference analyzer in removed.AnalyzerReferences)
            {
                context.RemoveAnalyzerReference(analyzer.FilePath);
            }

            foreach (CommandLineAnalyzerReference analyzer in added.AnalyzerReferences)
            {
                context.AddAnalyzerReference(analyzer.FilePath);
            }
        }
    }
}
