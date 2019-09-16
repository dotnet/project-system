// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSystemOptionsFactory
    {
        public static IProjectSystemOptions Create()
        {
            return Mock.Of<IProjectSystemOptions>();
        }

        public static IProjectSystemOptions ImplementIsProjectOutputPaneEnabled(Func<bool> action)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.SetupGet(o => o.IsProjectOutputPaneEnabled)
                .Returns(action);

            return mock.Object;
        }

        public static IProjectSystemOptions ImplementGetUseDesignerByDefaultAsync(Func<string, bool, CancellationToken, bool> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.GetUseDesignerByDefaultAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }

        public static IProjectSystemOptions ImplementSetUseDesignerByDefaultAsync(Func<string, bool, CancellationToken, Task> result)
        {
            var mock = new Mock<IProjectSystemOptions>();
            mock.Setup(o => o.SetUseDesignerByDefaultAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(result);

            return mock.Object;
        }
    }
}
