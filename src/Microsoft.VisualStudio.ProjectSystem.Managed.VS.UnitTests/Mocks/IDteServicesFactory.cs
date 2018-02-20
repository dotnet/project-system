// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using EnvDTE80;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IDteServicesFactory
    {
        public static IDteServices Create()
        {
            return Mock.Of<IDteServices>();
        }

        public static IDteServices ImplementSolution(Func<Solution2> solution)
        {
            var mock = new Mock<IDteServices>();
            mock.SetupGet(s => s.Solution)
                .Returns(solution);

            return mock.Object;
        }
    }
}
