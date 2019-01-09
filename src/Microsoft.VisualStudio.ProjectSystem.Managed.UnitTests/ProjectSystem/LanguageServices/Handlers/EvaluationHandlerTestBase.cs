// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public abstract class EvaluationHandlerTestBase
    {
        [Fact]
        public void Handle_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle(null, projectChange, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsProjectChange_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("projectChange", () =>
            {
                handler.Handle(10, null, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, projectChange, true, null);
            });
        }

        [Fact]
        public void Handle_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, projectChange, true, logger);
            });
        }

        [Fact]
        public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();

            var workspaceContext = IWorkspaceProjectContextMockFactory.Create();

            handler.Initialize(workspaceContext);

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Initialize(workspaceContext);
            });
        }

        internal static void Handle(IProjectEvaluationHandler handler, IProjectChangeDescription projectChange)
        {
            handler.Handle(1, projectChange, false, IProjectLoggerFactory.Create());
        }

        internal abstract IProjectEvaluationHandler CreateInstance();
    }
}
