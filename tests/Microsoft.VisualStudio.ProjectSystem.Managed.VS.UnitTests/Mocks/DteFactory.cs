// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;

namespace EnvDTE80
{
    internal static class DTEFactory
    {
        public static DTE2 Create()
        {
            var mock = new Mock<DTE>();

            return mock.As<DTE2>().Object;
        }

        public static DTE2 ImplementSolution(Func<Solution> action)
        {
            var mock = new Mock<DTE2>();
            mock.As<DTE>();

            mock.SetupGet(m => m.Solution)
                .Returns(action);

            return mock.Object;
        }
    }
}
