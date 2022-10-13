// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public abstract class SourceItemsHandlerTestBase
    {
        internal static void Handle(IWorkspaceProjectContext context, ISourceItemsHandler handler, IImmutableDictionary<string, IProjectChangeDescription> projectChanges)
        {
            handler.Handle(context, 1, projectChanges, new ContextState(), IManagedProjectDiagnosticOutputServiceFactory.Create());
        }

        internal abstract ISourceItemsHandler CreateInstance();
    }
}
