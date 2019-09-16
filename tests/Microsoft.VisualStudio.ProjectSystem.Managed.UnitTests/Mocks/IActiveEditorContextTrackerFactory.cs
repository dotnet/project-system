// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IActiveEditorContextTrackerFactory
    {
        public static IActiveEditorContextTracker Create()
        {
            return Mock.Of<IActiveEditorContextTracker>();
        }

        public static IActiveEditorContextTracker ImplementIsActiveEditorContext(Func<string, bool> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.IsActiveEditorContext(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static IActiveEditorContextTracker ImplementUnregisterContext(Action<string> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.UnregisterContext(It.IsAny<string>()))
                .Callback(action);

            return mock.Object;
        }

        public static IActiveEditorContextTracker ImplementRegisterContext(Action<string> action)
        {
            var mock = new Mock<IActiveEditorContextTracker>();
            mock.Setup(t => t.RegisterContext(It.IsAny<string>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
