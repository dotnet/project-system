// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;

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

        public static IActiveEditorContextTracker ImplementRegisterContext(Action<string> action, IDisposable? lifetime = null)
        {
            var mock = new Mock<IActiveEditorContextTracker>();

            mock.Setup(t => t.RegisterContext(It.IsAny<string>()))
                .Callback(action)
                .Returns(lifetime ?? EmptyDisposable.Instance);

            return mock.Object;
        }
    }
}
