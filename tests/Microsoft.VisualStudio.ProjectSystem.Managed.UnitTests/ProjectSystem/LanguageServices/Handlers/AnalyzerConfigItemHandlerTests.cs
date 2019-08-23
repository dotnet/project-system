// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class AnalyzerConfigItemHandlerTests : CommandLineHandlerTestBase
    {
        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static AnalyzerConfigItemHandler CreateInstance(UnconfiguredProject? project = null, IWorkspaceProjectContext? context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new AnalyzerConfigItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
