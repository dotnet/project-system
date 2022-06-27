// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceContextHandlerFactory
    {
        public static IWorkspaceContextHandler ImplementDispose(Action action)
        {
            var mock = new Mock<IWorkspaceContextHandler>();

            var disposable = mock.As<IDisposable>();
            disposable.Setup(x => x.Dispose())
                      .Callback(action);

            return mock.Object;
        }

        public static IWorkspaceContextHandler Create()
        {
            var mock = new Mock<IWorkspaceContextHandler>();
            return mock.Object;
        }
    }
}
