// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IActiveEditorContextTrackerFactory
    {
        public static IActiveEditorContextTracker Create()
        {
            return Mock.Of<IActiveEditorContextTracker>();
        }

        public static IActiveEditorContextTracker ImplementIsActiveEditorContext(Func<IWorkspaceProjectContext, bool> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.IsActiveEditorContext(It.IsAny<IWorkspaceProjectContext>()))
                .Returns(action);

            return mock.Object;
        }

        public static IActiveEditorContextTracker ImplementReleaseContext(Action<IWorkspaceProjectContext> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.UnregisterContext(It.IsAny<IWorkspaceProjectContext>()))
                .Callback(action);

            return mock.Object;
        }

        public static IActiveEditorContextTracker ImplementRegisterContext(Action<IWorkspaceProjectContext, string> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.RegisterContext(It.IsAny<IWorkspaceProjectContext>(), It.IsAny<string>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
