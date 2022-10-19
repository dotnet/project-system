// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    public class ActiveEditorContextTrackerTests
    {
        [Fact]
        public void IsActiveEditorContext_NullAsContextId_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("contextId", () =>
            {
                instance.IsActiveEditorContext(null!);
            });
        }

        [Fact]
        public void RegisterContext_EmptyAsContextId_ThrowsArgument()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentException>("contextId", () =>
            {
                instance.RegisterContext(string.Empty);
            });
        }

        [Theory]
        [InlineData(VSConstants.VSITEMID_NIL)]
        [InlineData(VSConstants.VSITEMID_SELECTION)]
        [InlineData(0)]
        public void GetProjectName_InvalidIdAsItemId_ReturnsInvalidArg(uint itemid)
        {
            var instance = CreateInstance();

            int result = instance.GetProjectName(itemid, out string? projectNameResult);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Null(projectNameResult);
        }

        [Fact]
        public void IsActiveEditorContext_NotRegisteredContextAsContextId_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.IsActiveEditorContext("NotRegistered");
            });
        }

        [Fact]
        public void RegisteredContext_AlreadyRegisteredContextAsContextId_ThrowsInvalidOperation()
        {
            var instance = CreateInstance();

            instance.RegisterContext("ContextId");

            Assert.Throws<InvalidOperationException>(() =>
            {
                instance.RegisterContext("ContextId");
            });
        }

        [Fact]
        public void UnregisterContext_RegisteredContextAsContextId_CanUnregister()
        {
            var instance = CreateInstance();

            var registration = instance.RegisterContext("ContextId");

            registration.Dispose();

            // Should be unregistered
            Assert.Throws<InvalidOperationException>(() => instance.IsActiveEditorContext("ContextId"));
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextIdNotSet_UsesFirstRegisteredContext(string contextId)
        {
            var instance = CreateInstance();

            instance.RegisterContext("ContextId");
            instance.RegisterContext(contextId);

            var result = instance.IsActiveEditorContext("ContextId");

            Assert.True(result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextIdSetToNull_UsesFirstRegisteredContext(string contextId)
        {
            var instance = CreateInstance();

            instance.RegisterContext("FirstContextId");
            instance.RegisterContext(contextId);

            // Set it the value first
            instance.ActiveIntellisenseProjectContext = contextId;

            // Now explicitly set to null
            instance.ActiveIntellisenseProjectContext = null;

            var result = instance.IsActiveEditorContext("FirstContextId");

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]           // Case-sensitive
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextIdDoesNotMatch_ReturnsFalse(string activeIntellisenseProjectContextId)
        {
            var instance = CreateInstance();

            instance.RegisterContext("ContextId");

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContextId;

            var result = instance.IsActiveEditorContext("ContextId");

            Assert.False(result);
        }

        [Fact]
        public void IsActiveEditorContext_WhenActiveIntellisenseProjectContextIdMatches_ReturnsTrue()
        {
            var instance = CreateInstance();

            instance.RegisterContext("ContextId");

            instance.ActiveIntellisenseProjectContext = "ContextId";

            var result = instance.IsActiveEditorContext("ContextId");

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ContextId")]
        public void GetProjectName_ReturnsActiveIntellisenseProjectContextId(string activeIntellisenseProjectContextId)
        {
            var instance = CreateInstance();

            instance.ActiveIntellisenseProjectContext = activeIntellisenseProjectContextId;

            instance.GetProjectName(HierarchyId.Root, out string? result);

            Assert.Equal(activeIntellisenseProjectContextId, result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]
        public void GetProjectName_WhenActiveIntellisenseProjectContextIdNotSet_ReturnsFirstRegisteredContext(string contextId)
        {
            var instance = CreateInstance();

            instance.RegisterContext("FirstContextId");
            instance.RegisterContext(contextId);

            instance.GetProjectName(HierarchyId.Root, out string? result);

            Assert.Equal("FirstContextId", result);
        }

        [Theory]
        [InlineData("AnotherContextId")]
        [InlineData("contextId")]
        public void GetProjectName_WhenActiveIntellisenseProjectContextIdSetToNull_ReturnsFirstRegisteredContext(string contextId)
        {
            var instance = CreateInstance();

            instance.RegisterContext("FirstContextId");
            instance.RegisterContext(contextId);

            // Set it the value first
            instance.ActiveIntellisenseProjectContext = contextId;

            // Now explicitly set to null
            instance.ActiveIntellisenseProjectContext = null;

            instance.GetProjectName(HierarchyId.Root, out string? result);

            Assert.Equal("FirstContextId", result);
        }

        private static VsActiveEditorContextTracker CreateInstance()
        {
            return new VsActiveEditorContextTracker(null, new ActiveEditorContextTracker(null));
        }
    }
}
