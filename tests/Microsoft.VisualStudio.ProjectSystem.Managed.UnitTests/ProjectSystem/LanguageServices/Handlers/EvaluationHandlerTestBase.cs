// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public abstract class EvaluationHandlerTestBase
    {
        [Fact]
        public void EvaluationRuleName_ReturnsValue()
        {
            var handler = CreateInstance();

            Assert.NotEmpty(handler.ProjectEvaluationRule);
        }

        [Fact]
        public void Handle_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle(null!, projectChange, new ContextState(), logger);
            });
        }

        [Fact]
        public void Handle_NullAsProjectChange_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<ArgumentNullException>("projectChange", () =>
            {
                handler.Handle(10, null!, new ContextState(), logger);
            });
        }

        [Fact]
        public void Handle_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, projectChange, new ContextState(), null!);
            });
        }

        [Fact]
        public void Handle_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, projectChange, new ContextState(), logger);
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
            handler.Handle(1, projectChange, new ContextState(), IProjectDiagnosticOutputServiceFactory.Create());
        }

        internal abstract IProjectEvaluationHandler CreateInstance();
    }
}
