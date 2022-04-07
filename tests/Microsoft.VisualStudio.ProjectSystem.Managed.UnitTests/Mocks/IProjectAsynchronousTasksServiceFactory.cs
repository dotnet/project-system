// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectAsynchronousTasksServiceFactory
    {
        public static IProjectAsynchronousTasksService Create()
        {
            return ImplementUnloadCancellationToken(CancellationToken.None);
        }

        public static IProjectAsynchronousTasksService ImplementUnloadCancellationToken(CancellationToken cancellationToken)
        {
            var mock = new Mock<IProjectAsynchronousTasksService>();

            mock.Setup(s => s.UnloadCancellationToken)
                .Returns(cancellationToken);

            return mock.Object;
        }
    }
}
