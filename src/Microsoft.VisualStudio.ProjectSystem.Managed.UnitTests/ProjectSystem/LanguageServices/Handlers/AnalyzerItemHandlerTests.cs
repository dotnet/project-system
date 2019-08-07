// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class AnalyzerItemHandlerTests : CommandLineHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new AnalyzerItemHandler((UnconfiguredProject)null);
            });
        }

        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private AnalyzerItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new AnalyzerItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
