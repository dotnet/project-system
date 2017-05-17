// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    internal class AdditionalFilesItemHandler : ICommandLineHandler
    {
        private readonly IWorkspaceProjectContext _context;

        public AdditionalFilesItemHandler(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _context = context;
        }

        public void Handle(BuildOptions added, BuildOptions removed, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                _context.RemoveAdditionalFile(additionalFile.Path);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                _context.AddAdditionalFile(additionalFile.Path, isInCurrentContext: isActiveContext);
            }
        }
    }
}
