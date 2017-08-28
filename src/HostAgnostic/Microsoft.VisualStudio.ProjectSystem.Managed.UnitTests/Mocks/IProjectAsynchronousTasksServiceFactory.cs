// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectAsynchronousTasksServiceFactory
    {
        public static IProjectAsynchronousTasksService Create(CancellationToken cancelToken)
        {
            var mock = new Mock<IProjectAsynchronousTasksService>();

            mock.Setup(s => s.UnloadCancellationToken).Returns(cancelToken);

            return mock.Object;
        }
    }
}
