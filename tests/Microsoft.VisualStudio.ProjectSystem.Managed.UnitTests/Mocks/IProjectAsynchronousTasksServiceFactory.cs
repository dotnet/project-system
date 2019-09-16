// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Moq;

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
