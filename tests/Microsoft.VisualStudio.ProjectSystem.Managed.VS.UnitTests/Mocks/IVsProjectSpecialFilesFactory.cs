// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsProjectSpecialFilesFactory
    {
        public static IVsProjectSpecialFiles ImplementGetFile(FuncWithOut<int, uint, uint, string, int> action)
        {
            var mock = new Mock<IVsHierarchy>().As<IVsProjectSpecialFiles>();

            uint itemId;
            string fileName;
            mock.Setup(s => s.GetFile(It.IsAny<int>(), It.IsAny<uint>(), out itemId, out fileName))
                .Returns(action);

            return mock.Object;
        }
    }
}
