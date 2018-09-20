// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IActiveWorkspaceProjectContextHostFactory
    {
        public static IActiveWorkspaceProjectContextHost Create()
        {
            return Mock.Of<IActiveWorkspaceProjectContextHost>();
        }

        public static IActiveWorkspaceProjectContextHost ImplementHostSpecificErrorReporter(Func<object> action)
        {
            var mock = new Mock<IActiveWorkspaceProjectContextHost>();
            mock.SetupGet(h => h.HostSpecificErrorReporter)
                .Returns(action);

            return mock.Object;
        }
    }
}
