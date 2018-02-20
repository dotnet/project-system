// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
    }
}
