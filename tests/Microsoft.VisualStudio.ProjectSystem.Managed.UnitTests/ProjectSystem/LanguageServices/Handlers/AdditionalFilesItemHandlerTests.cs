// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
