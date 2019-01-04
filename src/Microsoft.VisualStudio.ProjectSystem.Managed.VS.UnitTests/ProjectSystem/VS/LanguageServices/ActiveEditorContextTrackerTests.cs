// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    public class ActiveEditorContextTrackerTests
    {
        [Fact]
        public void IsActiveEditorContext_NullAsContext_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                instance.IsActiveEditorContext((IWorkspaceProjectContext)null);
            });
        }

        [Fact]
        public void RegisterContext_NullAsContext_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                instance.RegisterContext((IWorkspaceProjectContext)null, "ContextId");
            });
        }

        [Fact]
        public void RegisterContext_NullAsContextId_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentNullException>("contextId", () =>
            {
                instance.RegisterContext(context, (string)null);
            });
        }

        [Fact]
        public void RegisterContext_EmptyAsContextId_ThrowsArgument()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentException>("contextId", () =>
            {
                instance.RegisterContext(context, string.Empty);
            });
        }

        [Fact]
        public void UnregisterContext_NullAsContext_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                instance.UnregisterContext((IWorkspaceProjectContext)null);
            });
        }

        [Theory]
        [InlineData(VSConstants.VSITEMID_NIL)]
        [InlineData(VSConstants.VSITEMID_SELECTION)]
        [InlineData(0)]
        public void GetProjectName_InvalidIdAsItemId_ReturnsInvalidArg(uint itemid)
        {
            var instance = CreateInstance();

            int result = instance.GetProjectName(itemid, out string projectNameResult);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Null(projectNameResult);
        }

        [Fact]
        public void IsActiveEditorContext_UnregisteredContextAsContext_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.IsActiveEditorContext(context);
            });
        }

        [Fact]
        public void RegisteredContext_AlreadyRegisteredContextAsContext_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            instance.RegisterContext(context, "ContextId");

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.RegisterContext(context, "ContextId");
            });
        }

        [Fact]
        public void UnregisteredContext_UnregisteredContextAsContext_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.UnregisterContext(context);
            });
        }

        [Fact]
        public void UnregisterContext_RegisteredContextAsContext_CanUnregister()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            instance.RegisterContext(context, "ContextId");

            instance.UnregisterContext(context);

            // Should be unregistered
            Assert.Throws<InvalidOperationException>(() => instance.IsActiveEditorContext(context));
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextNotSet_UsesActiveHostContextId(string contextId)
        {
            var activeWorkspaceProjectContextHost = IActiveWorkspaceProjectContextHostFactory.ImplementContextId(contextId);
            var instance = CreateInstance(activeWorkspaceProjectContextHost);

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, contextId);

            var result = instance.IsActiveEditorContext(context);

            Assert.True(result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextSetToNull_UsesActiveHostContextId(string contextId)
        {
            var activeWorkspaceProjectContextHost = IActiveWorkspaceProjectContextHostFactory.ImplementContextId(contextId);
            var instance = CreateInstance(activeWorkspaceProjectContextHost);

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, contextId);

            // Set it the value first
            instance.ActiveIntellisenseProjectContext = contextId;

            // Now explicitly set to null
            instance.ActiveIntellisenseProjectContext = null;

            var result = instance.IsActiveEditorContext(context);

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextDoesNotMatch_ReturnsFalse(string activeIntellisenseProjectContext)
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, "ContextId");

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContext;

            var result = instance.IsActiveEditorContext(context);

            Assert.False(result);
        }

        [Fact]
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextMatches_ReturnsTrue()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, "ContextId");

            instance.ActiveIntellisenseProjectContext = "ContextId";

            var result = instance.IsActiveEditorContext(context);

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ContextId")]
        public void GetProjectName_ReturnsActiveIntellisenseProjectContext(string activeIntellisenseProjectContext)
        {
            var instance = CreateInstance();

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContext;

            instance.GetProjectName(HierarchyId.Root, out string result);

            Assert.Equal(activeIntellisenseProjectContext, result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]
        public void GetProjectName_WhenActiveIntellisenseProjectContextNotSet_ReturnsActiveHostContextId(string contextId)
        {
            var activeWorkspaceProjectContextHost = IActiveWorkspaceProjectContextHostFactory.ImplementContextId(contextId);
            var instance = CreateInstance(activeWorkspaceProjectContextHost);

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, contextId);

            instance.GetProjectName(HierarchyId.Root, out string result);

            Assert.Equal(contextId, result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]
        public void GetProjectName_WhenActiveIntellisenseProjectContextSetToNull_ReturnsActiveHostContextId(string contextId)
        {
            var activeWorkspaceProjectContextHost = IActiveWorkspaceProjectContextHostFactory.ImplementContextId(contextId);
            var instance = CreateInstance(activeWorkspaceProjectContextHost);

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, contextId);

            // Set it the value first
            instance.ActiveIntellisenseProjectContext = contextId;

            // Now explicitly set to null
            instance.ActiveIntellisenseProjectContext = null;

            instance.GetProjectName(HierarchyId.Root, out string result);

            Assert.Equal(contextId, result);
        }

        private static ActiveEditorContextTracker CreateInstance()
        {
            return CreateInstance(activeWorkspaceProjectContextHost: null);
        }

        private static ActiveEditorContextTracker CreateInstance(IActiveWorkspaceProjectContextHost activeWorkspaceProjectContextHost)
        {
            IProjectThreadingService threadingService = IProjectThreadingServiceFactory.Create();

            return new ActiveEditorContextTracker(threadingService, activeWorkspaceProjectContextHost);
        }
    }
}
