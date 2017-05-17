// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(AbstractContextHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class AdditionalFilesItemHandler : AbstractContextHandler, ICommandLineHandler
    {
        [ImportingConstructor]
        public AdditionalFilesItemHandler(UnconfiguredProject project)
        {
        }

        public void Handle(BuildOptions added, BuildOptions removed, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            EnsureInitialized();

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                Context.RemoveAdditionalFile(additionalFile.Path);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                Context.AddAdditionalFile(additionalFile.Path, isInCurrentContext: isActiveContext);
            }
        }
    }
}
