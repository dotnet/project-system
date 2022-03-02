// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public abstract class CommandLineHandlerTestBase
    {
        [Fact]
        public void Handle_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle(null!, added, removed, new ContextState(), logger);
            });
        }

        [Fact]
        public void Handle_NullAsAdded_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<ArgumentNullException>("added", () =>
            {
                handler.Handle(10, null!, removed, new ContextState(), logger);
            });
        }

        [Fact]
        public void Handle_NullAsRemoved_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<ArgumentNullException>("removed", () =>
            {
                handler.Handle(10, added, null!, new ContextState(), logger);
            });
        }

        [Fact]
        public void Handle_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, added, removed, new ContextState(), null!);
            });
        }

        [Fact]
        public void Handle_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectDiagnosticOutputService>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, added, removed, new ContextState(), logger);
            });
        }

        [Fact]
        public void Initialize_NullAsContext_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                handler.Initialize(null!);
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

        internal abstract ICommandLineHandler CreateInstance();
    }
}
