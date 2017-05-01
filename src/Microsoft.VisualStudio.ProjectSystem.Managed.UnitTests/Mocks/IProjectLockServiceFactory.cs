// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectLockServiceFactory
    {
        public static IProjectLockService Create()
        {
            var mock = new Mock<IProjectLockService>();

            mock.Setup(t => t.ReadLockAsync(It.IsAny<CancellationToken>())).Returns();

            return mock.Object;
        }
    }
}
