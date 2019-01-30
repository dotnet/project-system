// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    internal static class IWorkspaceProjectContextFactoryFactory
    {
        public static IWorkspaceProjectContextFactory Create()
        {
            return Mock.Of<IWorkspaceProjectContextFactory>();
        }

        public static IWorkspaceProjectContextFactory ImplementCreateProjectContext(Func<string, string, string, Guid, object, string, IWorkspaceProjectContext> action)
        {
            var mock = new Mock<IWorkspaceProjectContextFactory>();

            mock.Setup(c => c.CreateProjectContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
