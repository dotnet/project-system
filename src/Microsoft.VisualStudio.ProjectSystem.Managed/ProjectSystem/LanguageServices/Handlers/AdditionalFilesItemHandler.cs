// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(ICommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class AdditionalFilesItemHandler : ICommandLineHandler
    {
        [ImportingConstructor]
        public AdditionalFilesItemHandler(UnconfiguredProject project)
        {
        }

        public void Handle(BuildOptions added, BuildOptions removed, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                context.RemoveAdditionalFile(additionalFile.Path);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                context.AddAdditionalFile(additionalFile.Path, isInCurrentContext: isActiveContext);
            }
        }
    }
}
