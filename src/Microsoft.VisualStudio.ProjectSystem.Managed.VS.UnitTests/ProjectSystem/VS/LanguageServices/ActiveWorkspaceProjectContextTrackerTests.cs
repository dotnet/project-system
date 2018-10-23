// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    public class ActiveWorkspaceProjectContextTrackerTests
    {
        [Fact]
        public void IsActiveContext_NullAsContext_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                instance.IsActiveContext((IWorkspaceProjectContext)null);
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
        public void IsActiveContext_UnregisteredContextAsContext_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.IsActiveContext(context);
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
            Assert.Throws<InvalidOperationException>(() => instance.IsActiveContext(context));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveContext_WhenActiveIntellisenseProjectContextDoesNotMatch_ReturnsFalse(string activeIntellisenseProjectContext)
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, "ContextId");

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContext;

            var result = instance.IsActiveContext(context);

            Assert.False(result);
        }

        [Fact]
        public void IsActiveContext_WhenActiveIntellisenseProjectContextMatches_ReturnsTrue()
        {
            var instance = CreateInstance();

            var context = IWorkspaceProjectContextMockFactory.Create();
            instance.RegisterContext(context, "ContextId");

            instance.ActiveIntellisenseProjectContext = "ContextId";

            var result = instance.IsActiveContext(context);

            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ContextId")]
        public void GetProjectName_ReturnsActiveIntellisenseProjectContext(string activeIntellisenseProjectContext)
        {
            var instance = CreateInstance();

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContext;

            instance.GetProjectName(HierarchyId.Root, out string result);

            Assert.Equal(activeIntellisenseProjectContext, result);
        }

        private static ActiveWorkspaceProjectContextTracker CreateInstance()
        {
            return new ActiveWorkspaceProjectContextTracker(UnconfiguredProjectFactory.Create());
        }
    }
}
