// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class AdditionalFilesItemHandlerTests : CommandLineHandlerTestBase
    {
        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static AdditionalFilesItemHandler CreateInstance(UnconfiguredProject? project = null, IWorkspaceProjectContext? context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new AdditionalFilesItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
