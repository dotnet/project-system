// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class CompileItemHandler_EvaluationTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new CompileItemHandler(null!);
            });
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private static CompileItemHandler CreateInstance(UnconfiguredProject? project = null, IWorkspaceProjectContext? context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new CompileItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
