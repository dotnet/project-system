// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsWindowFrameFactory
    {
        public static IVsWindowFrame ImplementShow(Func<int> action)
        {
            var mock = new Mock<IVsWindowFrame>();
            mock.Setup(h => h.Show())
                .Returns(action());

            return mock.Object;
        }
    }
}
