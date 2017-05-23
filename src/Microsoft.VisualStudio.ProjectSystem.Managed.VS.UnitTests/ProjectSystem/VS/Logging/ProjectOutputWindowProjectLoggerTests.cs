// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    [ProjectSystemTrait]
    public class ProjectOutputWindowProjectLoggerTests
    {
        private static ProjectOutputWindowProjectLogger CreateInstance(IProjectThreadingService threadingService = null, IProjectSystemOptions options = null, IProjectOutputWindowPaneProvider outputWindowProvider = null)
        {
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            options = options ?? IProjectSystemOptionsFactory.Create();
            outputWindowProvider = outputWindowProvider ?? IProjectOutputWindowPaneProviderFactory.Create();

            return new ProjectOutputWindowProjectLogger(threadingService, options, outputWindowProvider);
        }
    }
}
