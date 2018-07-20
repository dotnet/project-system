// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Trait("UnitTest", "ProjectSystem")]
    public class SourceItemHandler_EvaluationTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new SourceItemHandler((UnconfiguredProject)null);
            });
        }

        internal override IEvaluationHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private SourceItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            var handler = new SourceItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
