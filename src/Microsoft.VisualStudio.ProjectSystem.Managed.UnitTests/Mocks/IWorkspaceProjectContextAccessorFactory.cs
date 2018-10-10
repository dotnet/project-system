// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceProjectContextAccessorFactory
    {
        public static IWorkspaceProjectContextAccessor ImplementContext(IWorkspaceProjectContext context)
        {
            var mock = new Mock<IWorkspaceProjectContextAccessor>();

            mock.Setup(c => c.Context)
                .Returns(context);

            return mock.Object;
        }

        public static IWorkspaceProjectContextAccessor Create()
        {
            return Mock.Of<IWorkspaceProjectContextAccessor>();
        }
    }
}
