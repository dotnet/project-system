// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceCommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class AdditionalFilesItemHandler : ILanguageServiceCommandLineHandler
    {
        private IWorkspaceProjectContext _context;

        [ImportingConstructor]
        public AdditionalFilesItemHandler(UnconfiguredProject project)
        {
        }

        public void SetContext(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _context = context;
        }

        public void Handle(CommandLineArguments added, CommandLineArguments removed)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                _context.RemoveAdditionalFile(additionalFile.Path);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                // TODO: IsInCurrentContext
                _context.AddAdditionalFile(additionalFile.Path);
            }
        }
    }
}
